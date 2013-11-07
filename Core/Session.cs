/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: Session.cs,v 1.2 2011/10/27 23:21:55 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

using Poderosa.Util.Collections;
using Poderosa.Plugins;
using Poderosa.Forms;
using Poderosa.Document;
using Poderosa.Util;

[assembly: PluginDeclaration(typeof(Poderosa.Sessions.SessionManagerPlugin))]

namespace Poderosa.Sessions {
    //PoderosaMainWindow���ĂԂ��߂̃C���^�t�F�[�X
    internal interface ISessionManagerForPoderosaWindow {
        PrepareCloseResult CloseMultipleDocuments(ClosingContext context, IPoderosaDocument[] documents);
    }

    internal interface ISessionManagerForViewSplitter {
        void ChangeLastAttachedViewForAllDocuments(IPoderosaView closing_view, IPoderosaView alternative);
        void ChangeLastAttachedViewForWindow(IPoderosaMainWindow window, IPoderosaView alternative);
        void SetLastAttachedViewForAllDocuments(IPoderosaView view);
    }

    [PluginInfo(ID = SessionManagerPlugin.PLUGIN_ID, Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.window")]
    internal class SessionManagerPlugin :
        PluginBase,
        ISessionManager,
        ISessionManagerForViewSplitter {
        public const string PLUGIN_ID = "org.poderosa.core.sessions";
        private static SessionManagerPlugin _instance;
        private TypedHashtable<ISession, SessionHost> _sessionMap;
        private TypedHashtable<IPoderosaDocument, DocumentHost> _documentMap;
        private ActivateContext _activateContext;
        private IExtensionPoint _docViewRelationHandler;
        private ListenerList<IActiveDocumentChangeListener> _activeDocumentChangeListeners;
        private ListenerList<ISessionListener> _sessionListeners;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            _instance = this;
            base.InitializePlugin(poderosa);
            _sessionMap = new TypedHashtable<ISession, SessionHost>();
            _documentMap = new TypedHashtable<IPoderosaDocument, DocumentHost>();
            _docViewRelationHandler = poderosa.PluginManager.CreateExtensionPoint("org.poderosa.core.sessions.docViewRelationHandler", typeof(IDocViewRelationEventHandler), this);
            _activeDocumentChangeListeners = new ListenerList<IActiveDocumentChangeListener>();
            _activeDocumentChangeListeners.Add(new WindowCaptionManager());

            _sessionListeners = new ListenerList<ISessionListener>();
        }
        public override void TerminatePlugin() {
            base.TerminatePlugin();
            //����͖�������Ă��Ȃ��ƁI
            Debug.Assert(_sessionMap.Count == 0);
            Debug.Assert(_documentMap.Count == 0);
        }

        public static SessionManagerPlugin Instance {
            get {
                return _instance;
            }
        }

        public IEnumerable<ISession> AllSessions {
            get {
                List<ISession> r = new List<ISession>();
                foreach (ISession session in _sessionMap.Keys)
                    r.Add(session);
                return r;
            }
        }

        public IPoderosaDocument[] GetDocuments(IPoderosaMainWindow window) {
            List<IPoderosaDocument> r = new List<IPoderosaDocument>();
            foreach (DocumentHost dh in _documentMap.Values) {
                if (dh.LastAttachedView != null && dh.LastAttachedView.ParentForm == window)
                    r.Add(dh.Document);
            }
            return r.ToArray();
        }

        public void StartNewSession(ISession session, IPoderosaView firstView) {
            firstView = AdjustToOuterView(firstView);
            SessionHost host = new SessionHost(this, session);
            _sessionMap.Add(session, host);
            session.InternalStart(host);
            foreach (ISessionListener listener in _sessionListeners)
                listener.OnSessionStart(session);

            //���̎��_�ŁA���Ȃ��Ƃ���h�L�������g���Ȃ��Ƃ����Ȃ��B�Q�ȏ�͕s�A�ł�������������Ȃ�
            if (host.DocumentCount == 0)
                throw new InvalidOperationException("session must register at least one document in InternalStart()");
            AttachDocumentAndView(host.DocumentAt(0), firstView);
        }

        public void AttachDocumentAndView(IPoderosaDocument document, IPoderosaView view) {
            view = AdjustToOuterView(view);
            DocumentHost dh = FindDocumentHost(document);
            Debug.Assert(dh != null, "the document must be registered by calling ISessionHost#RegisterDocument");

            if (view.Document == document) {
                Debug.Assert(dh.CurrentView == view);
                return; //�������Ȃ�
            }

            IPoderosaView previous_view = dh.CurrentView; //�֘A�Â����w�肷��h�L�������g�����Ƃ��ƌ����Ă����r���[
            IPoderosaForm last_window = ViewToForm(dh.LastAttachedView); //���Ƃ��Ƃ̏��L�E�B���h�E�B���߂Ă�Attach�ł�null�ł��邱�Ƃɂ��イ��

            //���݂̊֘A����U�؂�
            if (previous_view != null) {
                Debug.WriteLineIf(DebugOpt.DumpDocumentRelation, "Detach Prev View " + ViewName(previous_view));
                dh.DetachView();
            }
            Debug.Assert(dh.CurrentView == null);

            //�ڑ���Ƀh�L�������g�����݂��Ă���΂����؂藣��
            IPoderosaDocument existing_doc = view.Document;
            if (existing_doc != null) { //�Ώۂ̃r���[�ɌÂ��̂��Ђ����Ă�����O��
                DocumentHost eh = FindDocumentHost(existing_doc);
                Debug.Assert(eh.CurrentView == view);
                Debug.WriteLineIf(DebugOpt.DumpDocumentRelation, String.Format("Detach Destination View doc={0} view={1}", existing_doc.GetType().Name, ViewName(view)));
                eh.DetachView();
            }

            //�V�K�̐ڑ�
            Debug.Assert(view.Document == null && dh.CurrentView == null); //Attach�������ł��Ă��邱�Ɗm�F
            dh.AttachView(view);

            //�ړ����邱�ƂŐV�K�Ɍ�����悤�ɂȂ�h�L�������g��T��
            if (previous_view != null && previous_view != view) {
                DocumentHost new_visible_doc = ShowBackgroundDocument(previous_view);
                Debug.Assert(new_visible_doc != dh);
            }

            //�h�L�������g��ۗL����E�B���h�E���ω�������ʒm�B����Attach�ł�last_mainwindow==null�ł��邱�Ƃɒ���
            if (last_window != view.ParentForm) {
                if (last_window != null)
                    NotifyRemove(last_window, document);
                NotifyAdd(ViewToForm(view), document);
            }

            FireDocViewRelationChange();
        }

        //ISessionManager�̏I���n�@�ׂ����̂̓h�L�������g����
        public PrepareCloseResult CloseDocument(IPoderosaDocument document) {
            DocumentHost dh = FindDocumentHost(document);
            Debug.Assert(dh != null);
            SessionHost sh = dh.SessionHost;
            PrepareCloseResult r = sh.Session.PrepareCloseDocument(document);
            if (r == PrepareCloseResult.Cancel)
                return r;

            CleanupDocument(dh);
            if (r == PrepareCloseResult.TerminateSession || sh.DocumentCount == 0)
                CleanupSession(sh);
            return r;
        }

        public PrepareCloseResult TerminateSession(ISession session) {
            SessionHost sh = FindSessionHost(session);
            PrepareCloseResult r = sh.Session.PrepareCloseSession();
            Debug.Assert(r != PrepareCloseResult.ContinueSession, "sanity");
            if (r == PrepareCloseResult.Cancel)
                return r;

            CleanupSession(sh);
            return r;
        }

        public PrepareCloseResult CloseMultipleDocuments(ClosingContext context, IPoderosaDocument[] documents) {
            List<SessionHost> sessions = CreateSessionHostCollection();
            foreach (SessionHost sh in sessions)
                sh.CMP_ClosingDocumentCount = 0; //�J�E���g���Z�b�g

            foreach (IPoderosaDocument doc in documents) {
                DocumentHost dh = FindDocumentHost(doc);
                dh.SessionHost.CMP_ClosingDocumentCount++;
            }
            //�����܂łŁA�eSessionHost���Ƃɉ��̃h�L�������g����悤�Ƃ��Ă��邩���J�E���g���ꂽ�B
            //���ɂ��ꂼ��ɂ��ď������͂��߂�
            PrepareCloseResult result = PrepareCloseResult.TerminateSession;
            foreach (SessionHost sh in sessions) {
                if (sh.CMP_ClosingDocumentCount == 0)
                    continue; //�e���Ȃ�

                if (sh.CMP_ClosingDocumentCount == sh.DocumentCount) { //�Z�b�V�����̑S�h�L�������g�����ꍇ
                    PrepareCloseResult r = TerminateSession(sh.Session);
                    sh.CMP_PrepareCloseResult = r;
                    if (r == PrepareCloseResult.TerminateSession)
                        context.AddClosingSession(sh);
                    else if (r == PrepareCloseResult.Cancel)
                        result = PrepareCloseResult.Cancel; //��ł��L�����Z��������ΑS�̂��L�����Z���Ƃ���
                }
                else { //�ꕔ�̃h�L�������g�����B���ꂪ�ʓ|
                    //TODO unsupported
                    Debug.Assert(false, "unsupported");
                }
            }

            //�����ɂ��ăZ�b�V���������
            foreach (SessionHost sh in context.ClosingSessions) {
                CleanupSession(sh);
            }

            return result;
        }

        public void RefreshDocumentStatus(IPoderosaDocument document) {
            DocumentHost dh = FindDocumentHost(document);
            Debug.Assert(dh != null);
            IPoderosaForm f = ViewToForm(dh.LastAttachedView);

            //������Ɖ�������
            IPoderosaMainWindow mw = (IPoderosaMainWindow)f.GetAdapter(typeof(IPoderosaMainWindow));
            if (mw != null)
                mw.DocumentTabFeature.Update(document);
            else
                ((IPoderosaPopupWindow)f.GetAdapter(typeof(IPoderosaPopupWindow))).UpdateStatus();
        }

        private void CleanupDocument(DocumentHost dh) {
            IPoderosaForm owner_window = ViewToForm(dh.LastAttachedView);
            IPoderosaView visible_view = dh.CurrentView;
            bool was_active = false;
            if (visible_view != null) {
                was_active = visible_view.AsControl().Focused;
                dh.DetachView();
                FireDocViewRelationChange();
            }

            if (owner_window != null) {
                NotifyRemove(owner_window, dh.Document);
            }

            dh.SessionHost.CloseDocument(dh.Document);
            _documentMap.Remove(dh.Document);

            //�����h�L�������g�̃r���[�������Ă����ꍇ�́A���̈ʒu�̕ʂ̃h�L�������g��������
            //TODO �E�B���h�E�����Ƃ��͂��̏����͕s�v
            if (visible_view != null && visible_view.ParentForm.GetAdapter(typeof(IPoderosaMainWindow)) != null) {
                ShowBackgroundDocument(visible_view);
                if (was_active && visible_view.Document != null)
                    ActivateDocument(visible_view.Document, ActivateReason.InternalAction);
            }
        }
        internal void CleanupSession(SessionHost sh) { //SessionHost������Ă΂��̂�internal
            foreach (ISessionListener listener in _sessionListeners)
                listener.OnSessionEnd(sh.Session);
            foreach (IPoderosaDocument doc in sh.ClonedDocuments) {
                CleanupDocument(FindDocumentHost(doc));
            }
            sh.Session.InternalTerminate();
            _sessionMap.Remove(sh.Session);
        }

        /**
         * Activate�����̃��[�g
         * NOTE �d���R�[���͂����Ńu���b�N����悤�ɂ���B
         * �A�N�e�B�u�ȃh�L�������g���ω�����̂́A
         *   - View���N���b�N���ăt�H�[�J�X���ς��Ƃ�
         *   - �^�u���N���b�N�����Ƃ�
         *   - �L�[�{�[�h�V���[�g�J�b�g���APoderosa�̃R�[�h����������Ƃ�
         * �̂R�B
         * ���̂����̂ǂ�ł��邩���w�肵�Ă������ĂԁB�Ⴆ�΁AFocus�ړ��̂Ƃ��͉��߂�Focus()���Ă΂Ȃ��ȂǓ����ŏꍇ�������Ȃ����
         */
        public void ActivateDocument(IPoderosaDocument document, ActivateReason reason) {
            Debug.Assert(document != null);

            //�l�X�g�̖h�~ Focus�n�C�x���g�n���h��������Ƃǂ����Ă��Ă΂�Ă��܂��̂�
            if (_activateContext != null)
                return;

            try {
                _activateContext = new ActivateContext(document, reason);

                DocumentHost dh = FindDocumentHost(document);
                Debug.Assert(dh != null);

                if (dh.CurrentView != null) { //���Ɍ����Ă���ꍇ
                    if (reason != ActivateReason.ViewGotFocus)
                        SetFocusToView(dh.CurrentView); //���[�U�̃t�H�[�J�X�w�肾�����ꍇ�͂���ɔC����
                }
                else { //�����Ă͂��Ȃ������ꍇ
                    IPoderosaView view = dh.LastAttachedView;
                    Debug.Assert(view != null); //�������������d�g�݂��ǂ����ɂق��������B���͂��ׂĂ�Document���ŏ���AttachDocumentAndView����邱�Ƃ�z�肵�Ă���
                    AttachDocumentAndView(document, view);
                    Debug.Assert(dh.CurrentView == view);
                    if (!view.AsControl().Focused)
                        view.AsControl().Focus();
                }

                Debug.Assert(dh.CurrentView.Document == document);


                //�ʒm
                NotifyActivation(ViewToForm(dh.CurrentView), document, reason);
            }
            finally {
                _activateContext = null;
                if (DebugOpt.DumpDocumentRelation)
                    DumpDocumentRelation();
            }
        }

        //SessionHost����Ă΂��n��
        public void RegisterDocument(IPoderosaDocument document, SessionHost sessionHost) {
            _documentMap.Add(document, new DocumentHost(this, sessionHost, document));
        }

        public DocumentHost FindDocumentHost(IPoderosaDocument document) {
            return _documentMap[document];
        }
        public SessionHost FindSessionHost(ISession session) {
            return _sessionMap[session];
        }

        internal IEnumerable<DocumentHost> GetAllDocumentHosts() {
            return new ConvertingEnumerable<DocumentHost>(_documentMap.Values);
        }

        //View�̃}�[�W�ł�Activate����
        public void ChangeLastAttachedViewForAllDocuments(IPoderosaView closing_view, IPoderosaView alternative) {
            closing_view = AdjustToOuterView(closing_view);
            alternative = AdjustToOuterView(alternative);
            foreach (DocumentHost dh in _documentMap.Values) {
                if (dh.LastAttachedView == closing_view) {
                    dh.AlternateView(alternative);
                    FireDocViewRelationChange();
                }
            }
        }
        public void ChangeLastAttachedViewForWindow(IPoderosaMainWindow window, IPoderosaView alternative) {
            alternative = AdjustToOuterView(alternative);
            foreach (DocumentHost dh in _documentMap.Values) {
                if (dh.LastAttachedView.ParentForm == window) {
                    dh.AlternateView(alternative);
                    FireDocViewRelationChange();
                }
            }
        }

        public void SetLastAttachedViewForAllDocuments(IPoderosaView view) {
            view = AdjustToOuterView(view);
            foreach (DocumentHost dh in _documentMap.Values) {
                dh.AlternateView(view);
            }
            FireDocViewRelationChange();
        }

        //view�̈ʒu�ɂ���V�K�̃h�L�������g��������悤�ɂ���B�h�L�������g�̕\���ʒu��ς����Ƃ��A�����Ƃ��Ɏ��s����
        private DocumentHost ShowBackgroundDocument(IPoderosaView view) {
            DocumentHost new_visible_doc = FindNewVisibleDoc(view);
            if (new_visible_doc != null) {
                new_visible_doc.AttachView(view);
            }
            return new_visible_doc;
        }

        private List<SessionHost> CreateSessionHostCollection() {
            List<SessionHost> r = new List<SessionHost>();
            foreach (object t in _sessionMap.Values)
                r.Add((SessionHost)t);
            return r;
        }

        private DocumentHost FindNewVisibleDoc(IPoderosaView view) {
            view = AdjustToOuterView(view);
            //TODO ����͂��������B���̃��[�����A�^�u�ł̏��ԁA�A�N�e�B�u�ɂȂ������Ԃ��L���A�ȂǕ����̎�i���l�����邵�A�v���O�C���Ŋg�����ׂ��Ƃ���
            foreach (DocumentHost dh in _documentMap.Values) {
                if (dh.LastAttachedView == view)
                    return dh;
            }
            return null;
        }

        private IPoderosaForm ViewToForm(IPoderosaView view) {
            if (view == null)
                return null;
            else {
                return (IPoderosaForm)view.ParentForm.GetAdapter(typeof(IPoderosaForm));
            }
        }

        private void FireDocViewRelationChange() {
            foreach (IDocViewRelationEventHandler eh in _docViewRelationHandler.GetExtensions())
                eh.OnDocViewRelationChange();
        }

        //Listener
        public void AddActiveDocumentChangeListener(IActiveDocumentChangeListener listener) {
            _activeDocumentChangeListeners.Add(listener);
        }
        public void RemoveActiveDocumentChangeListener(IActiveDocumentChangeListener listener) {
            _activeDocumentChangeListeners.Remove(listener);
        }
        public void AddSessionListener(ISessionListener listener) {
            _sessionListeners.Add(listener);
        }
        public void RemoveSessionListener(ISessionListener listener) {
            _sessionListeners.Remove(listener);
        }

        private void NotifyActivation(IPoderosaForm form, IPoderosaDocument document, ActivateReason reason) {
            Debug.Assert(document != null);
            IPoderosaMainWindow window = (IPoderosaMainWindow)form.GetAdapter(typeof(IPoderosaMainWindow));

            if (window != null) {
                //Tab�ւ̒ʒm�BTabClick�̂Ƃ���Tab�����O�ŏ������Ă�̂�OK
                if (reason != ActivateReason.TabClick)
                    window.DocumentTabFeature.Activate(document);
                //listener�ւ̒ʒm
                foreach (IActiveDocumentChangeListener listener in _activeDocumentChangeListeners)
                    listener.OnDocumentActivated(window, document);
            }
        }

        private void NotifyAdd(IPoderosaForm form, IPoderosaDocument document) {
            IPoderosaMainWindow window = (IPoderosaMainWindow)form.GetAdapter(typeof(IPoderosaMainWindow));
            if (window != null)
                window.DocumentTabFeature.Add(document);
        }

        private void NotifyRemove(IPoderosaForm form, IPoderosaDocument document) {
            IPoderosaMainWindow window = (IPoderosaMainWindow)form.GetAdapter(typeof(IPoderosaMainWindow));
            if (window != null) {
                IPoderosaDocument former = window.DocumentTabFeature.ActiveDocument;
                window.DocumentTabFeature.Remove(document);
                //TODO �A�N�e�B�u�Ȃ̂��L������ꏊ��ς��邱�ƂŃ^�u�ւ̒ʒm���ɂ��鐧�񂩂��������
                if (former == document) {
                    foreach (IActiveDocumentChangeListener listener in _activeDocumentChangeListeners)
                        listener.OnDocumentDeactivated(window);
                }
            }
        }

        private IPoderosaDocument ViewToActiveDocument(IPoderosaView view) {
            IPoderosaForm form = view.ParentForm;
            IPoderosaMainWindow window = (IPoderosaMainWindow)form.GetAdapter(typeof(IPoderosaMainWindow));
            if (window != null)
                return window.DocumentTabFeature.ActiveDocument;
            else
                return view.Document;
        }

        //�r���[�Ƀt�H�[�J�X���Z�b�g������Ԃɂ���B�|�b�v�A�b�v�E�B���h�E�̏ꍇ�A�܂��E�B���h�E�����[�h����Ă��Ȃ��P�[�X������̂ł����ɒ��ӁI
        private void SetFocusToView(IPoderosaView view) {
            IPoderosaForm form = view.ParentForm;
            IPoderosaPopupWindow popup = (IPoderosaPopupWindow)form.GetAdapter(typeof(IPoderosaPopupWindow));
            if (popup != null) {
                if (!popup.AsForm().Visible) {
                    popup.UpdateStatus();
                    popup.AsForm().Show();
                }
            }

            if (!view.AsControl().Focused)
                view.AsControl().Focus(); //���ɃE�B���h�E�͌����Ă���
        }

        private static IPoderosaView AdjustToOuterView(IPoderosaView view) {
            //ContentReplaceableSite������΂��̐e���g�p����
            IContentReplaceableViewSite s = (IContentReplaceableViewSite)view.GetAdapter(typeof(IContentReplaceableViewSite));
            if (s != null)
                return s.CurrentContentReplaceableView;
            else
                return view;
        }

        private void DumpDocumentRelation() {
            Debug.WriteLine("[DocRelation]");
            foreach (DocumentHost dh in _documentMap.Values) {
                Debug.WriteLine(String.Format("  doc {0}, current={1}, last={2}", dh.Document.GetType().Name, ViewName(dh.CurrentView), ViewName(dh.LastAttachedView)));
            }
        }
        private static string ViewName(IPoderosaView view) {
            if (view == null)
                return "null";
            else {
                IContentReplaceableView rv = (IContentReplaceableView)view.GetAdapter(typeof(IContentReplaceableView));
                if (rv != null)
                    return rv.GetCurrentContent().GetType().Name;
                else
                    return view.GetType().Name;
            }
        }
    }

    internal class SessionHost : ISessionHost {
        private SessionManagerPlugin _parent;
        private ISession _session;
        private List<IPoderosaDocument> _documents;

        //�ȉ��̃����o��SessionManager#CloseMultipleDocuments�ɂ̂ݎg�p�B
        //�X���b�h�Z�[�t�ł͂Ȃ��Ȃ邪�������ɖ��͂Ȃ����낤
        private int _closingDocumentCount;
        private PrepareCloseResult _prepareCloseResult;

        public SessionHost(SessionManagerPlugin parent, ISession session) {
            _parent = parent;
            _session = session;
            _documents = new List<IPoderosaDocument>();
        }

        public ISession Session {
            get {
                return _session;
            }
        }
        public int DocumentCount {
            get {
                return _documents.Count;
            }
        }
        public IEnumerable<IPoderosaDocument> Documents {
            get {
                return _documents;
            }
        }
        public IPoderosaDocument DocumentAt(int index) {
            return _documents[index];
        }
        public IEnumerable<IPoderosaDocument> ClonedDocuments {
            get {
                return new List<IPoderosaDocument>(_documents);
            }
        }

        #region ISessionHost
        public void RegisterDocument(IPoderosaDocument document) {
            _parent.RegisterDocument(document, this);
            _documents.Add(document);
        }
        //�z�X�g���Ă���Z�b�V�����������I�ɏI������ꍇ
        public void TerminateSession() {
            _parent.CleanupSession(this);
        }
        public IPoderosaForm GetParentFormFor(IPoderosaDocument document) {
            DocumentHost dh = _parent.FindDocumentHost(document);
            Debug.Assert(dh != null, "document must be alive");
            IPoderosaView view = dh.LastAttachedView;
            if (view != null)
                return view.ParentForm; //���ꂪ���݂���Ȃ�OK
            else
                return WindowManagerPlugin.Instance.ActiveWindow; //������Ɣ����C���̎�������
        }
        #endregion

        public void CloseDocument(IPoderosaDocument document) {
            Debug.Assert(_documents.Contains(document));
            _session.InternalCloseDocument(document);
            _documents.Remove(document);
        }

        //�ȉ���CloseMultipleDocument���Ŏg�p����
        public int CMP_ClosingDocumentCount {
            get {
                return _closingDocumentCount;
            }
            set {
                _closingDocumentCount = value;
            }
        }
        public PrepareCloseResult CMP_PrepareCloseResult {
            get {
                return _prepareCloseResult;
            }
            set {
                _prepareCloseResult = value;
            }
        }
    }

    internal class DocumentHost {
        private SessionManagerPlugin _manager;
        private SessionHost _sessionHost;
        private IPoderosaDocument _document;
        private IPoderosaView _currentView;
        private IPoderosaView _lastAttachedView;

        public DocumentHost(SessionManagerPlugin manager, SessionHost sessionHost, IPoderosaDocument document) {
            _manager = manager;
            _sessionHost = sessionHost;
            _document = document;
        }

        public IPoderosaView LastAttachedView {
            get {
                return _lastAttachedView;
            }
        }

        public IPoderosaView CurrentView {
            get {
                return _currentView;
            }
        }
        public SessionHost SessionHost {
            get {
                return _sessionHost;
            }
        }
        public IPoderosaDocument Document {
            get {
                return _document;
            }
        }

        //�r���[�Ƃ̊֘A�t���ύX
        public void AttachView(IPoderosaView view) {
            _lastAttachedView = view;
            _currentView = view;

            IViewFactory vf = WindowManagerPlugin.Instance.ViewFactoryManager.GetViewFactoryByDoc(_document.GetType());
            IContentReplaceableView rv = (IContentReplaceableView)view.GetAdapter(typeof(IContentReplaceableView));
            IPoderosaView internalview = rv == null ? view : rv.AssureViewClass(vf.GetViewType()); //ContentReplaceableView�̂Ƃ��͒��g���g�p
            Debug.Assert(vf.GetViewType() == internalview.GetType());
            _sessionHost.Session.InternalAttachView(_document, internalview);
        }
        public void DetachView() {
            Debug.Assert(_currentView != null);
            IContentReplaceableView rv = (IContentReplaceableView)_currentView.GetAdapter(typeof(IContentReplaceableView));
            IPoderosaView internalview = rv == null ? _currentView : rv.GetCurrentContent(); //ContentReplaceableView�̂Ƃ��͒��g���g�p
            _sessionHost.Session.InternalDetachView(_document, internalview);

            if (rv != null && rv.AsControl().Visible)
                rv.AssureEmptyViewClass();

            _currentView = null;
        }

        //View��������Ȃǂő�ւ̃r���[�ɒu������
        public void AlternateView(IPoderosaView view) {
            if (_currentView != null)
                DetachView();
            _lastAttachedView = view;
        }



    }

    internal class ClosingContext {
        private enum CloseType {
            OneDocument,
            OneSession,
            OneWindow,
            AllWindows
        }

        private CloseType _type;
        private IPoderosaMainWindow _window; //_type==OneWindow�̂Ƃ��̂݃Z�b�g�A���̂Ƃ���null
        private List<SessionHost> _closingSessions;

        public ClosingContext(IPoderosaMainWindow window) {
            _type = CloseType.OneWindow;
            _window = window;
            _closingSessions = new List<SessionHost>();
        }

        public void AddClosingSession(SessionHost sh) {
            Debug.Assert(_type == CloseType.OneWindow);
            _closingSessions.Add(sh);
        }
        public IEnumerable<SessionHost> ClosingSessions {
            get {
                return _closingSessions;
            }
        }
    }

    internal class ActivateContext {
        private IPoderosaDocument _document;
        private ActivateReason _reason;

        public ActivateContext(IPoderosaDocument document, ActivateReason reason) {
            _document = document;
            _reason = reason;
        }
    }
}
