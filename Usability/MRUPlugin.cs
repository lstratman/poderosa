/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MRUPlugin.cs,v 1.4 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using Poderosa.Util.Collections;
using Poderosa.Forms;
using Poderosa.Protocols;
using Poderosa.Sessions;
using Poderosa.Terminal;
using Poderosa.Plugins;
using Poderosa.Preferences;
using Poderosa.Serializing;
using Poderosa.Commands;

[assembly: PluginDeclaration(typeof(Poderosa.Usability.MRUPlugin))]

namespace Poderosa.Usability {
    [PluginInfo(ID = MRUPlugin.PLUGIN_ID, Version = VersionInfo.PODEROSA_VERSION, Author = VersionInfo.PROJECT_NAME, Dependencies = "org.poderosa.core.serializing;org.poderosa.terminalsessions")]
    internal class MRUPlugin : PluginBase {
        public const string PLUGIN_ID = "org.poderosa.usability.mru";

        public static MRUPlugin Instance;
        private MRUList _mruList;
        private OpenMRUCommand _mruCommand;
        private ICoreServices _coreServices;
        private IProtocolService _protocolService;
        private ITerminalSessionsService _terminalSessionsService;
        private MRUOptionsSupplier _optionSupplier;

        public override void InitializePlugin(IPoderosaWorld poderosa) {
            base.InitializePlugin(poderosa);

            Instance = this;

            IPluginManager pm = poderosa.PluginManager;
            _optionSupplier = new MRUOptionsSupplier();
            _coreServices = (ICoreServices)poderosa.GetAdapter(typeof(ICoreServices));
            _coreServices.PreferenceExtensionPoint.RegisterExtension(_optionSupplier);

            _protocolService = (IProtocolService)pm.FindPlugin("org.poderosa.protocols", typeof(IProtocolService));
            _terminalSessionsService = (ITerminalSessionsService)pm.FindPlugin("org.poderosa.terminalsessions", typeof(ITerminalSessionsService));

            //�ڑ��������Ƀ��X�g�X�V
            _mruList = new MRUList(this, pm);
            _coreServices.SessionManager.AddSessionListener(_mruList);
            pm.FindExtensionPoint("org.poderosa.menu.file").RegisterExtension(_mruList);
            pm.FindExtensionPoint("org.poderosa.terminalsessions.telnetSSHLoginDialogInitializer").RegisterExtension(_mruList);
            pm.FindExtensionPoint("org.poderosa.terminalsessions.loginDialogUISupport").RegisterExtension(_mruList);

            _mruCommand = new OpenMRUCommand();
        }

        public OpenMRUCommand OpenMRUCommand {
            get {
                return _mruCommand;
            }
        }
        public MRUOptionsSupplier OptionSupplier {
            get {
                return _optionSupplier;
            }
        }
        public ISessionManager SessionManager {
            get {
                return _coreServices.SessionManager;
            }
        }
        public IProtocolService ProtocolService {
            get {
                return _protocolService;
            }
        }
        public ITerminalSessionsService TerminalSessionsService {
            get {
                return _terminalSessionsService;
            }
        }
        public IWindowManager WindowManager {
            get {
                return _coreServices.WindowManager;
            }
        }

        #region IPreferenceSupplier
        public string ID {
            get {
                return PLUGIN_ID;
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            IIntPreferenceItem limitCount = builder.DefineIntValue(folder, "limitcount", 5, PreferenceValidatorUtil.PositiveIntegerValidator); //����l
            builder.DefineLooseNode(folder, _mruList, "list");
        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            return null;
        }

        public string GetDescription(IPreferenceItem item) {
            return "MRU";
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }
        #endregion

        public MRUList MRUList {
            get {
                return _mruList;
            }
        }
    }


    /// <summary>
    /// <ja>
    /// MRU�iMost Recently Used�F�ŋߎg�������́j�̃I�v�V������ݒ肷��C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// It is an interface that sets the option of MRU (Most Recently Used).
    /// </en>
    /// </summary>
    public interface IMRUOptions {
        /// <summary>
        /// <ja>�ێ����闚�𐔂ł��B</ja>
        /// <en>Maintained number of histories</en>
        /// </summary>
        int LimitCount {
            get;
            set;
        }
    }

    internal class MRUItem : IAdaptable {
        private ITerminalParameter _terminalParam;
        private ITerminalSettings _terminalSettings;
        private StructuredText _lateBindContent; //���ꂪnull�łȂ��Ƃ��͒x�����[�h�̕K�v����

        public MRUItem(ITerminalSession ts) {
            _terminalParam = ts.TerminalTransmission.Connection.Destination;
            _terminalSettings = ts.TerminalSettings;
            _lateBindContent = null;
        }
        public MRUItem(ITerminalParameter tp, ITerminalSettings ts) {
            _terminalParam = tp;
            _terminalSettings = ts;
            _lateBindContent = null;
        }
        public MRUItem(StructuredText latebindcontent) {
            _terminalParam = null;
            _terminalSettings = null;
            _lateBindContent = latebindcontent;
        }

        public ITerminalParameter TerminalParameter {
            get {
                AssureContent();
                return _terminalParam;
            }
        }
        public ITerminalSettings TerminalSettings {
            get {
                AssureContent();
                return _terminalSettings;
            }
        }
        public void IsolateSettings() {
            AssureContent();
            //TerminalParam, Settings���ꂼ��ŃN���[�������悤�ɕω�������
            _terminalParam = (ITerminalParameter)_terminalParam.Clone();
            _terminalSettings = _terminalSettings.Clone();
        }

        private void AssureContent() {
            if (_lateBindContent == null)
                return;

            MRUItem temp = MRUItemSerializer.Instance.Deserialize(_lateBindContent) as MRUItem;
            Debug.Assert(temp != null); //�^�`�F�b�N���炢�̓��[�h���ɂ��Ă���
            _terminalParam = temp._terminalParam;
            _terminalSettings = temp._terminalSettings;
            _lateBindContent = null; //����Œx������
        }

        public IAdaptable GetAdapter(Type adapter) {
            return MRUPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
    }

    internal class MRUItemSerializer : ISerializeServiceElement {
        private static MRUItemSerializer _instance;
        private ISerializeService _serializeService;

        public static MRUItemSerializer Instance {
            get {
                return _instance;
            }
        }

        public MRUItemSerializer(IPluginManager pm) {
            _instance = this;
            _serializeService = (ISerializeService)pm.FindPlugin("org.poderosa.core.serializing", typeof(ISerializeService));
            pm.FindExtensionPoint("org.poderosa.core.serializeElement").RegisterExtension(this);
            Debug.Assert(_serializeService != null);
        }

        public Type ConcreteType {
            get {
                return typeof(MRUItem);
            }
        }


        public StructuredText Serialize(object obj) {
            MRUItem item = (MRUItem)obj;
            StructuredText t = new StructuredText(this.ConcreteType.FullName);
            t.AddChild(_serializeService.Serialize(item.TerminalParameter));
            t.AddChild(_serializeService.Serialize(item.TerminalSettings));
            return t;
        }

        public object Deserialize(StructuredText node) {
            //TODO �G���[�n���h�����O�ア
            if (node.ChildCount != 2)
                return null;
            return new MRUItem(
                (ITerminalParameter)_serializeService.Deserialize(node.GetChildOrNull(0)),
                (ITerminalSettings)_serializeService.Deserialize(node.GetChildOrNull(1)));
        }

    }


    internal class MRUList :
        IPreferenceLooseNodeContent,
        ISessionListener,
        IPoderosaMenuGroup,
        IPositionDesignation,
        ITelnetSSHLoginDialogInitializer,
        ILoginDialogUISupport {
        private MRUItemSerializer _serializer;
        private OrderedCollection<MRUItem> _data; //�擪�ɂ����قǐ�ɐڑ��������̂Ƃ݂Ȃ�
        private MRUPlugin _parent;

        public MRUList(MRUPlugin parent, IPluginManager pm) {
            _parent = parent;
            _serializer = new MRUItemSerializer(pm);
            _data = new OrderedCollection<MRUItem>(MRUItemEquality);
        }

        #region ISessionListener
        public void OnSessionStart(ISession session) {
            ITerminalSession ts = (ITerminalSession)session.GetAdapter(typeof(ITerminalSession));
            if (ts == null
                || HasExcludeFromMRUAttribute(ts)
                || HasExcludeFromMRUAttribute(ts.TerminalTransmission.Connection.Destination)) {

                return;
            }

            _data.Update(new MRUItem(ts));
            int limit = MRUPlugin.Instance.OptionSupplier.OriginalOptions.LimitCount;
            _data.LimitCount(limit);

            ////Volatile�ɂ��邩����Reload�X�^�C��
            //MRUPlugin.Instance.WindowManager.ReloadMenu("org.poderosa.menu.file");
        }

        private bool HasExcludeFromMRUAttribute(object obj) {
            return obj != null && obj.GetType().GetCustomAttributes(typeof(ExcludeFromMRUAttribute), false).Length != 0;
        }

        public void OnSessionEnd(ISession session) {
            //do nothing
        }
        #endregion

        #region IPreferenceLooseNodeContent
        public void Reset() {
            _data.Clear();
        }
        public IPreferenceLooseNodeContent Clone() {
            return this; //TODO ���ڂ�B���̓R�s�[�ɑ΂��ĕҏW����悤�Ȃ��Ƃ��Ȃ��̂ł���ł��\��Ȃ���
        }

        public void LoadFrom(StructuredText node) {
            _data.Clear();
            string classname = typeof(MRUItem).FullName;
            foreach (StructuredText item in node.Children) {
                try {
                    //�N���������̂��ߒx���f�V���A���C�Y
                    if (item.Name == classname) {
                        _data.Add(new MRUItem(item));
                    }
                }
                catch (Exception ex) {
                    RuntimeUtil.ReportException(ex);
                }
            }
        }

        public void SaveTo(StructuredText node) {
            foreach (MRUItem tp in _data) {
                try {
                    node.AddChild(_serializer.Serialize(tp));
                }
                catch (Exception ex) {
                    RuntimeUtil.ReportException(ex);
                }
            }
        }
        #endregion

        #region IPoderosaMenuGroup
        public IPoderosaMenu[] ChildMenus {
            get {
                return CreateMenus();
            }
        }

        public bool IsVolatileContent {
            get {
                return true;
            }
        }

        public bool ShowSeparator {
            get {
                return true;
            }
        }
        #endregion

        #region IAdaptable
        public IAdaptable GetAdapter(Type adapter) {
            return _parent.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
        #endregion

        #region IPositionDesignation
        public IAdaptable DesignationTarget {
            get {
                return null;
            }
        }

        public PositionType DesignationPosition {
            get {
                return PositionType.Last;
            }
        }
        #endregion

        #region ITelnetSSHLoginDialogInitializer
        public void ApplyLoginDialogInfo(ITelnetSSHLoginDialogInitializeInfo info) {

            foreach (MRUItem item in _data) {
                ITerminalParameter tp = item.TerminalParameter;
                ITCPParameter tcp = (ITCPParameter)tp.GetAdapter(typeof(ITCPParameter));
                ISSHLoginParameter ssh = (ISSHLoginParameter)tp.GetAdapter(typeof(ISSHLoginParameter));
                ICygwinParameter cygwin = (ICygwinParameter)tp.GetAdapter(typeof(ICygwinParameter));
                if (tcp != null)
                    info.AddHost(tcp.Destination);
                if (tcp != null)
                    info.AddPort(tcp.Port);
                if (ssh != null)
                    info.AddIdentityFile(ssh.IdentityFileName);
                if (ssh != null)
                    info.AddAccount(ssh.Account);
            }
        }

        #endregion

        #region ILoginDialogUISupport
        public void FillTopDestination(Type adapter, out ITerminalParameter parameter, out ITerminalSettings settings) {
            Debug.WriteLineIf(DebugOpt.MRU, "FillTop");
            FillCorrespondingDestination(adapter, null, out parameter, out settings);
        }

        public void FillCorrespondingDestination(Type adapter, string destination, out ITerminalParameter parameter, out ITerminalSettings settings) {
            Debug.WriteLineIf(DebugOpt.MRU, "FillCorrespondingDestination");
            List<ITerminalParameter> l = new List<ITerminalParameter>();
            foreach (MRUItem item in _data) {
                ITerminalParameter tp = item.TerminalParameter;
                if (tp.GetAdapter(adapter) != null) {
                    if (CheckDestination(item.TerminalParameter, destination)) { //��������
                        parameter = item.TerminalParameter;
                        settings = item.TerminalSettings;
                        return;
                    }
                }
            }

            parameter = null;
            settings = null;
        }
        private static bool CheckDestination(ITerminalParameter tp, string destination) {
            //destination��null(FillTop()�R��)�Ȃ���OK
            if (destination == null)
                return true;
            ITCPParameter tcp = (ITCPParameter)tp.GetAdapter(typeof(ITCPParameter));
            if (tcp == null)
                return false;
            else
                return tcp.Destination == destination;
        }

        #endregion

        private bool MRUItemEquality(MRUItem item1, MRUItem item2) {
            ITerminalParameter p1 = item1.TerminalParameter;
            ITerminalParameter p2 = item2.TerminalParameter;

            return p1.UIEquals(p2);
        }

        private IPoderosaMenuItem[] CreateMenus() {
            List<IPoderosaMenuItem> t = new List<IPoderosaMenuItem>();
            int index = 0;
            foreach (MRUItem item in _data)
                t.Add(new MRUMenuItem(index++, item));
            return t.ToArray();
        }

        private class MRUMenuItem : IPoderosaMenuItemWithArgs {

            private MRUItem _mruItem;
            private int _index;

            public MRUMenuItem(int index, MRUItem item) {
                _index = index;
                _mruItem = item;
            }

            public IPoderosaCommand AssociatedCommand {
                get {
                    return MRUPlugin.Instance.OpenMRUCommand;
                }
            }

            public string Text {
                get {
                    return String.Format("&{0} {1}", _index + 1, FormatItemDescription(_mruItem));
                }
            }

            public bool IsEnabled(ICommandTarget target) {
                return true;
            }

            public bool IsChecked(ICommandTarget target) {
                return false;
            }

            public IAdaptable[] AdditionalArgs {
                get {
                    return new IAdaptable[] { _mruItem };
                }
            }

            public IAdaptable GetAdapter(Type adapter) {
                return MRUPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
            }
        }

        public static string FormatItemDescription(MRUItem item) {
            ITerminalParameter param = item.TerminalParameter;
            string suffix = "";
            ICygwinParameter cygwin = (ICygwinParameter)param.GetAdapter(typeof(ICygwinParameter));
            if (cygwin != null)
                suffix = "- Cygwin";

            ISSHLoginParameter ssh = (ISSHLoginParameter)param.GetAdapter(typeof(ISSHLoginParameter));
            ITCPParameter tcp = (ITCPParameter)param.GetAdapter(typeof(ITCPParameter));
            if (ssh != null)
                suffix = String.Format("- {0}", ssh.Method.ToString());
            else if (tcp != null)
                suffix = "- Telnet";

            return String.Format("{0} {1}", item.TerminalSettings.Caption, suffix);
        }

        /*
        public static string FormatTerminalParameterDescription(ITerminalParameter param) {
            ICygwinParameter cygwin = (ICygwinParameter)param.GetAdapter(typeof(ICygwinParameter));
            if(cygwin!=null) return String.Format("{0} - cygwin", cygwin.Home);

            ISSHLoginParameter ssh = (ISSHLoginParameter)param.GetAdapter(typeof(ISSHLoginParameter));
            ITCPParameter tcp = (ITCPParameter)param.GetAdapter(typeof(ITCPParameter));
            if(ssh!=null) return String.Format("{0} - {1}", tcp.Destination, ssh.Method.ToString());

            Debug.Assert(tcp!=null);
            return String.Format("{0} - Telnet", tcp.Destination);
        }
         */


    }

    //����̓V���[�g�J�b�g�L�[������킯�ł͂Ȃ��̂Ŗ���
    internal class OpenMRUCommand : IPoderosaCommand {
        public CommandResult InternalExecute(ICommandTarget target, params IAdaptable[] args) {
            Debug.Assert(args != null && args.Length == 1);
            MRUItem item = (MRUItem)args[0].GetAdapter(typeof(MRUItem));

            //�R�}���h���s���_��Isolate����B
            //�Ȃ��Ȃ�ATerminalSettings��Session���ƂɃR�s�[�������Ȃ��Ƃ����Ȃ��i�Z�b�V�����Ԃ̋��L��NG�j���A
            //������Setting��ڑ���ɕύX�����炻��͕ۑ�����MRU�ɔ��f�������B
            //���ʂƂ��āA����MRUItem�𕡐���C���X�^���V�G�[�g������A�Ō�ɊJ�����ڑ���TerminalSettings��MRU�f�[�^�Ƃ��ĕۑ������B
            item.IsolateSettings();
            ISSHLoginParameter ssh = (ISSHLoginParameter)item.TerminalParameter.GetAdapter(typeof(ISSHLoginParameter));
            if (ssh != null)
                ssh.PasswordOrPassphrase = ""; //MRU�����SSH�N���̓p�X���[�h���͂͊O���Ȃ��B�I�v�V�����ŏȗ����ɂ��Ă�������

            ITerminalSession ts = MRUPlugin.Instance.TerminalSessionsService.TerminalSessionStartCommand.StartTerminalSession(target, item.TerminalParameter, item.TerminalSettings);
            return ts != null ? CommandResult.Succeeded : CommandResult.Failed;
        }
        public bool CanExecute(ICommandTarget target) {
            return true;
        }
        #region IAdaptable
        public IAdaptable GetAdapter(Type adapter) {
            return MRUPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }
        #endregion
    }


    //MRU����T�C�Y�ݒ�
    internal class MRUOptions : SnapshotAwarePreferenceBase, IMRUOptions {
        private IIntPreferenceItem _limitCount;

        public MRUOptions(IPreferenceFolder folder)
            : base(folder) {
        }

        public override void DefineItems(IPreferenceBuilder builder) {
            _limitCount = builder.DefineIntValue(_folder, "limitCount", 5,
                delegate(int value, IPreferenceValidationResult result) {
                    if (value < 0 || value > 100)
                        result.ErrorMessage = "MRU LimitCount Error"; //���ꂿ���ƌĂ΂�邩��
                });

        }
        public MRUOptions Import(MRUOptions src) {
            _limitCount = ConvertItem(src._limitCount);
            return this;
        }


        public int LimitCount {
            get {
                return _limitCount.Value;
            }
            set {
                _limitCount.Value = value;
            }
        }
    }

    internal class MRUOptionsSupplier : IPreferenceSupplier {
        private IPreferenceFolder _originalFolder;
        private MRUOptions _originalOptions;


        public MRUOptionsSupplier() {
        }

        public string PreferenceID {
            get {
                return MRUPlugin.PLUGIN_ID; //�����Ƃ���
            }
        }

        public void InitializePreference(IPreferenceBuilder builder, IPreferenceFolder folder) {
            _originalFolder = folder;
            _originalOptions = new MRUOptions(_originalFolder);
            _originalOptions.DefineItems(builder);

            MRUList mruList = MRUPlugin.Instance.MRUList;
            builder.DefineLooseNode(folder, mruList, "list");

        }

        public object QueryAdapter(IPreferenceFolder folder, Type type) {
            Debug.Assert(_originalFolder.Id == folder.Id);
            if (type == typeof(IMRUOptions))
                return _originalFolder == folder ? _originalOptions : new MRUOptions(folder).Import(_originalOptions);
            else
                return null;
        }

        public void ValidateFolder(IPreferenceFolder folder, IPreferenceValidationResult output) {
        }

        public IMRUOptions OriginalOptions {
            get {
                return _originalOptions;
            }
        }
    }

}
