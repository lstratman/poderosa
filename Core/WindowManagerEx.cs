/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: WindowManagerEx.cs,v 1.4 2012/03/18 11:02:29 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text;
using Poderosa.Document;
using Poderosa.Sessions;
using Poderosa.Commands;

using Poderosa.View;
using Poderosa.Util;

namespace Poderosa.Forms {

    //System.Windows.Forms.Control�Ɠ��������A�K�v�Ȃ��݂̂̂𒊏o�������
    /// <summary>
    /// <ja>
    /// �E�B���h�E��.NET Framework�̃R���g���[���Ƃ��Ĉ������߂̃C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface to treat the window as a control of .NET Framework. 
    /// </en>
    /// </summary>
    public interface IPoderosaControl : IAdaptable {
        /// <summary>
        /// <ja>.NET Framework��Control�I�u�W�F�N�g�ɕϊ����܂��B</ja>
        /// <en>Convert to the Control object of .NET Framework</en>
        /// </summary>
        /// <returns><ja>�ϊ����ꂽControl�I�u�W�F�N�g</ja><en>Converted Control object.</en></returns>
        Control AsControl();
    }

    /// <summary>
    /// <ja>
    /// �E�B���h�E��.NET Framework�̃t�H�[���Ƃ��Ĉ������߂̃C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface to treat the window as a form of .NET Framework. 
    /// </en>
    /// </summary>
    public interface IPoderosaForm : IPoderosaControl, ICommandTarget {
        /// <summary>
        /// <ja>
        /// .NET Framework��Form�I�u�W�F�N�g�ɕϊ����܂��B
        /// </ja>
        /// <en>Convert to the Form object of .NET Framework</en>
        /// </summary>
        /// <returns><ja>�ϊ����ꂽForm�I�u�W�F�N�g</ja><en>Converted Form object.</en></returns>
        Form AsForm();
        /// <summary>
        /// <ja>
        /// �E�B���h�E����܂��B
        /// </ja>
        /// <en>
        /// Close the window.
        /// </en>
        /// </summary>
        /// <returns><ja>����ꂽ���ǂ����������܂��B����ɕ���ꂽ�ꍇ�ACommandResult.Succeeded���Ԃ���܂��B</ja><en>Whether it was closed is shown. CommandResult.Succeeded is returned when close normally. </en></returns>
        /// <remarks>
        /// <ja>
        /// �Z�b�V�����̏����ɂ���ẮA���[�U�[�ɕ��Ă��悢���ǂ�����₢���킹�邱�Ƃ��ł��邽�߁A���铮�삪�L�����Z������邱�Ƃ�����܂��B
        /// �L�����Z�����ꂽ���ǂ����́A�߂�l�Ŕ��f���Ă��������B
        /// </ja>
        /// <en>
        /// Because it can be inquired whether I may close to the user according to the processing of the session, the closing operation might be canceled. 
        /// Please judge whether to have been canceled from the return value. 
        /// </en>
        /// </remarks>
        CommandResult CancellableClose();

        //�|�b�v�A�b�v���j���[
        /// <summary>
        /// <ja>
        /// �|�b�v�A�b�v���j���[�i�R���e�L�X�g���j���[�j��\�����܂��B
        /// </ja>
        /// <en>
        /// Show the popup menu (context menu).
        /// </en>
        /// </summary>
        /// <param name="menus"><ja>�\�����郁�j���[���������j���[�O���[�v�ł��B</ja><en>It is a menu group that shows the displayed menu. </en></param>
        /// <param name="target"><ja>���j���[�̃^�[�Q�b�g�ł��B</ja><en>It is a target of the menu. </en></param>
        /// <param name="point_screen"><ja>�\������ʒu�ł��B</ja><en>It is a displayed position. </en></param>
        /// <param name="flags"><ja>���j���[��\������Ƃ��ɐ擪�̍��ڂ�I����Ԃɂ��邩�ǂ����̃t���O�ł�</ja><en>Flag whether to put the first item into state of selection when menu is displayed</en></param>
        void ShowContextMenu(IPoderosaMenuGroup[] menus, ICommandTarget target, Point point_screen, ContextMenuFlags flags);

        //���[�U�ɑ΂���x���n�B����Form�����L���Ă��Ȃ��X���b�h������Ă΂�邱�Ƃ��l�����邱�� Note: �ʃC���^�t�F�[�X�ɕ������邩�H
        /// <summary>
        /// <ja>
        /// �x�����b�Z�[�W�{�b�N�X��\�����܂��B
        /// </ja>
        /// <en>
        /// Show the message box of warning.
        /// </en>
        /// </summary>
        /// <param name="msg"><ja>�\�����郁�b�Z�[�W�ł��B</ja><en>message to display</en></param>
        /// <remarks>
        /// <ja>
        /// <para>
        /// ���̃��\�b�h�́A�t�H�[�������L����X���b�h�ȊO����Ăяo���Ă����܂��܂���B
        /// </para>
        /// <para>
        /// �������I�u�W�F�N�g�ւ̃��b�N���������܂܌Ăяo���ƁA���̃��b�N�̓��b�Z�[�W�{�b�N�X�����܂ŉ������܂���B
        /// </para>
        /// </ja>
        /// <en>
        /// <para>
        /// You may call this method excluding the thread to own the form. 
        /// </para>
        /// <para>
        /// However, when the lock to the object is called while had, the lock is not released until the message box is closed. 
        /// </para>
        /// </en>
        /// </remarks>
        void Warning(string msg);
        /// <summary>
        /// <ja>
        /// ��񃁃b�Z�[�W�{�b�N�X��\�����܂��B
        /// </ja>
        /// <en>
        /// Show the message box of information.
        /// </en>
        /// </summary>
        /// <param name="msg"><ja>�\�����郁�b�Z�[�W�ł��B</ja><en>message to display</en></param>
        /// <remarks>
        /// <ja>
        /// <para>
        /// ���̃��\�b�h�́A�t�H�[�������L����X���b�h�ȊO����Ăяo���Ă����܂��܂���B
        /// </para>
        /// <para>
        /// �������I�u�W�F�N�g�ւ̃��b�N���������܂܌Ăяo���ƁA���̃��b�N�̓��b�Z�[�W�{�b�N�X�����܂ŉ������܂���B
        /// </para>
        /// </ja>
        /// <en>
        /// <para>
        /// You may call this method excluding the thread to own the form. 
        /// </para>
        /// <para>
        /// However, when the lock to the object is called while had, the lock is not released until the message box is closed. 
        /// </para>
        /// </en>
        /// </remarks>
        void Information(string msg);
        /// <summary>
        /// <ja>
        /// �m�͂��n���m�������n����q�˂郁�b�Z�[�W�{�b�N�X��\�����܂��B
        /// </ja>
        /// <en>
        /// Show the messaage box that asks "Yes" or "No".
        /// </en>
        /// </summary>
        /// <param name="msg"><ja>�\�����郁�b�Z�[�W�ł��B</ja><en>message to display</en></param>
        /// <returns><ja>�ǂ̃{�^���������ꂽ�̂��������l�ł��B�m�͂��n�̂Ƃ��ɂ�DialogResult.Yes�A�m�������n�̂Ƃ��ɂ�DialogResult.No�ƂȂ�܂��B</ja><en>It is a value in which which button was pushed is shown.When DialogResult.Yes getting it at "Yes", at the time of good it becomes DialogResult.No. </en></returns>
        /// <remarks>
        /// <ja>
        /// <para>
        /// ���̃��\�b�h�́A�t�H�[�������L����X���b�h�ȊO����Ăяo���Ă����܂��܂���B
        /// </para>
        /// <para>
        /// �������I�u�W�F�N�g�ւ̃��b�N���������܂܌Ăяo���ƁA���̃��b�N�̓��b�Z�[�W�{�b�N�X�����܂ŉ������܂���B
        /// </para>
        /// </ja>
        /// <en>
        /// <para>
        /// You may call this method excluding the thread to own the form. 
        /// </para>
        /// <para>
        /// However, when the lock to the object is called while had, the lock is not released until the message box is closed. 
        /// </para>
        /// </en>
        /// </remarks>
        DialogResult AskUserYesNo(string msg);
    }

    /// <summary>
    /// <ja>
    /// �R���e�L�X�g���j���[��\������Ƃ��̃t���O�������܂��B
    /// </ja>
    /// <en>
    /// The flag when the context menu is displayed is shown. 
    /// </en>
    /// </summary>
    [Flags]
    public enum ContextMenuFlags {
        /// <summary>
        /// <ja>
        /// �\�����ɉ������܂���B
        /// </ja>
        /// <en>
        /// Do nothing when displayed.
        /// </en>
        /// </summary>
        None = 0,
        /// <summary>
        /// <ja>
        /// �\�����ɐ擪�̍��ڂ�I�����ꂽ��Ԃɂ��܂��B
        /// </ja>
        /// <en>
        /// It puts it into the state that the first item was selected when displaying it. 
        /// </en>
        /// </summary>
        SelectFirstItem = 1
    }

    /// <summary>
    /// <ja>
    /// �E�B���h�E�}�l�[�W����ID�ł��B
    /// </ja>
    /// <en>
    /// ID of Window manager.
    /// </en>
    /// </summary>
    /// <exclude/>
    public class WindowManagerConstants {
        public const string MAINWINDOWCONTENT_ID = "org.poderosa.core.window.mainWindowContent";
        public const string VIEW_FACTORY_ID = "org.poderosa.core.window.viewFactory";
        public const string VIEWFORMATEVENTHANDLER_ID = "org.poderosa.core.window.viewFormatEventHandler";
        public const string TOOLBARCOMPONENT_ID = "org.poderosa.core.window.toolbar";
        public const string MAINWINDOWEVENTHANDLER_ID = "org.poderosa.core.window.mainWindowEventHandler";
        public const string FILEDROPHANDLER_ID = "org.poderosa.core.window.fileDropHandler";
    }

    //WindowManager�S��
    /// <summary>
    /// <ja>
    /// �E�B���h�E�}�l�[�W���������C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface that show the window manager.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// <para>
    /// �E�B���h�E�}�l�[�W���́A�v���O�C��ID�u<c>org.poderosa.core.window</c>�v�����v���O�C���Œ񋟂���Ă��܂��B
    /// </para>
    /// <para>
    /// ���̂悤��<seealso cref="Poderosa.Plugins.ICoreServices">ICoreServices</seealso>���o�R���Ď擾�ł��܂��B
    /// </para>
    /// <code>
    /// // ICoreServices���擾
    /// ICoreServices cs = (ICoreServices)PoderosaWorld.GetAdapter(typeof(ICoreServices));
    /// // IWindowManager���擾
    /// IWindowManager wm = cs.WindowManager;
    /// </code>
    /// </ja>
    /// <en>
    /// <para>
    /// Window manager is offered as the plug-in with plug-in ID [<c>org.poderosa.core.window</c>].
    /// </para>
    /// <para>
    /// <ja>���̂悤��<seealso cref="Poderosa.Plugins.ICoreServices">ICoreServices</seealso>���o�R���Ď擾�ł��܂��B</ja><en>It is possible to acquire it via <seealso cref="Poderosa.Plugins.ICoreServices">ICoreServices</seealso> as follows. </en>
    /// </para>
    /// <code>
    /// // Get ICoreServices
    /// ICoreServices cs = (ICoreServices)PoderosaWorld.GetAdapter(typeof(ICoreServices));
    /// // Get IWindowManager
    /// IWindowManager wm = cs.WindowManager;
    /// </code>
    /// </en>
    /// </remarks>
    public interface IWindowManager : IAdaptable {
        /// <summary>
        /// <ja>
        /// ���ׂẴ��C���E�B���h�E�������z��ł��B
        /// </ja>
        /// <en>
        /// Array that shows all the main windows.
        /// </en>
        /// </summary>
        IPoderosaMainWindow[] MainWindows {
            get;
        }
        /// <summary>
        /// <ja>
        /// �A�N�e�B�u�ȃE�B���h�E�������܂��B
        /// </ja>
        /// <en>
        /// Show the active window.
        /// </en>
        /// </summary>
        IPoderosaMainWindow ActiveWindow {
            get;
        }

        /// <summary>
        /// <ja>
        /// �I�u�W�F�N�g�̑I���ƃR�s�[�Ɋւ���A�N�Z�X��񋟂���ISelectionService��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// ISelectionService that offers the selection of the object and the access concerning the copy is returned. 
        /// </en>
        /// </summary>
        ISelectionService SelectionService {
            get;
        }

        //PopupView�쐬 : �R���ō쐬�����r���[�́A�Z�b�V�����}�l�[�W����AttachDocAndView->Activate�����邱�Ƃŏ��߂Č�����悤�ɂȂ�BCreatePopupView�����ł͌�����悤�ɂ͂Ȃ�Ȃ����Ƃɒ���
        /// <summary>
        /// <ja>
        /// �|�b�v�A�b�v�r���[���쐬���܂��B
        /// </ja>
        /// <en>
        /// Create the popup view.
        /// </en>
        /// </summary>
        /// <param name="viewcreation"><ja>�|�b�v�A�b�v�r���[���쐬����ۂ̃p�����[�^�ł�</ja><en>It is a parameter when the pop up view is made. </en></param>
        /// <returns><ja>�쐬���ꂽ�|�b�v�A�b�v�E�B���h�E���Ԃ���܂��B</ja><en>Return the made pop up window.</en></returns>
        /// <remarks>
        /// <ja>
        /// �쐬���ꂽ�r���[�́A�Z�b�V�����}�l�[�W���i<seealso cref="ISessionManager">ISessionManager</seealso>�j��
        /// <see cref="ISessionManager.AttachDocumentAndView">AttachDocumentAndView���\�b�h</see>���Ăяo���ăh�L�������g�ƃr���[���A�^�b�`���Ă���A
        /// �A�N�e�B�x�[�g���邱�ƂŁA���߂Č�����悤�ɂȂ�܂��B���̃��\�b�h�ō쐬���������ł͌�����悤�ɂ͂Ȃ�܂���B
        /// </ja>
        /// <en>
        /// It comes to see the made view for the first time by the activate after session manager(<seealso cref="ISessionManager">ISessionManager</seealso>)'s <see cref="ISessionManager.AttachDocumentAndView">AttachDocumentAndView method</see> is called and the document and the view are activated. It doesn't come to see it only by making it by this method. 
        /// </en>
        /// </remarks>
        /// <exclude/>
        IPoderosaPopupWindow CreatePopupView(PopupViewCreationParam viewcreation);

        //Reload
        /// <summary>
        /// <ja>
        /// ���j���[�������[�h���܂��B
        /// </ja>
        /// <en>
        /// Reload the menu.
        /// </en>
        /// </summary>
        void ReloadMenu();

        //Preference�n�̃����[�h
        /// <summary>
        /// <ja>�w�肵���A�Z���u���Ɋւ��郆�[�U�[�ݒ�l�iPreference�j���ēǍ����܂��B</ja>
        /// <en>User setting value (Preference) concerning the specified assembly is read again. </en>
        /// </summary>
        /// <param name="preference"><ja>�ēǍ�������ICoreServicePreference</ja><en>ICoreServicePreference to read again.</en></param>
        /// <overloads>
        /// <summary>
        /// <ja>
        /// ���[�U�[�ݒ�l�iPreference�j���ēǍ����܂��B
        /// </ja>
        /// <en>
        /// User setting value (Preference) is read again. 
        /// </en>
        /// </summary>
        /// </overloads>
        void ReloadPreference(ICoreServicePreference preference);

        /// <summary>
        /// <ja>
        /// ���[�U�[�ݒ�l�iPreference�j���ēǍ����܂��B
        /// </ja>
        /// <en>
        /// User setting value (Preference) is read again. 
        /// </en>
        /// </summary>
        void ReloadPreference();

		bool InvisibleMode {
			get;
			set;
		}

		StartMode StartMode {
			get;
			set;
		}
    }

    //�A�v���S�̂Ɋ֌W���A����system.Windows.Forms�����
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IWinFormsService : IAdaptable {
        //Timer Support
        ITimerSite CreateTimer(int interval, TimerDelegate callback);

        //Drag & Drop
        object GetDraggingObject(IDataObject data, Type required_type);
        void BypassDragEnter(Control target, DragEventArgs args);
        void BypassDragDrop(Control target, DragEventArgs args);
    }

    //event handler
    public interface IMainWindowEventHandler : IAdaptable {
        void OnFirstMainWindowLoaded(IPoderosaMainWindow window);
        void OnMainWindowLoaded(IPoderosaMainWindow window);
        void OnMainWindowUnloaded(IPoderosaMainWindow window);
        void OnLastMainWindowUnloaded(IPoderosaMainWindow window);
    }

    //File Drop : �t�@�C���ȊO���������Ƃ͂܂��Ȃ����낤����l���Ȃ��B���̂Ƃ��͂܂��ʂ̃C���^�t�F�[�X�ŁB
    public interface IFileDropHandler : IAdaptable {
        bool CanAccept(ICommandTarget target, string[] filenames);
        void DoDropAction(ICommandTarget target, string[] filenames);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IViewManagerFactory : IAdaptable {
        IViewManager Create(IPoderosaMainWindow parent);
        IViewFactory DefaultViewFactory {
            get;
            set;
        }
    }

    /// <summary>
    /// <ja>
    /// Poderosa�̃��C���E�B���h�E�������C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface that show the main window of Poderosa.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// ���j���[��c�[���o�[����Ăяo�����R�}���h�ł́A�^�[�Q�b�g�̓��C���E�B���h�E�ł��B���̂���GetAdapter���\�b�h���Ăяo�����ƂŁAIPoderosaMainWindow�ւƕϊ��ł��܂��B
    /// <code>
    /// // <var>target</var>�̓R�}���h�Ɉ����n���ꂽ�^�[�Q�b�g�ł���Ƒz�肵�܂��B
    /// IPoderosaMainWindow window = 
    ///     (IPoderosaMainWindow)target.GetAdapter(typeof(IPoderosaMainWindow));
    /// </code>
    /// �������̓E�B���h�E�}�l�[�W���i<seealso cref="IWindowManager">IWindowManager</seealso>�j��<see cref="IWindowManager.ActiveWindow">ActiveWindow�v���p�e�B</see>��
    /// �g���āA�A�N�e�B�u�ȃE�B���h�E���擾���邱�Ƃ��ł��܂��B
    /// <code>
    /// // cs��<seealso cref="Poderosa.Plugins.ICoreServices">ICoreServices</seealso>�������Ă���Ƒz�肵�܂��B
    /// IPoderosaMainWindow mainwin = cs.WindowManager.ActiveWindow;
    /// </code>
    /// </ja>
    /// <en>
    /// In the command called from the menu and the toolbar, the target is the main window. Therefore, it is possible to convert it into IPoderosaMainWindow by calling the GetAdapter method. 
    /// <code>
    /// // It is assumed that <var>target</var> is a target handed over to the command. 
    /// IPoderosaMainWindow window = 
    ///     (IPoderosaMainWindow)target.GetAdapter(typeof(IPoderosaMainWindow));
    /// </code>
    /// Or, an active window can be acquired by using window manager(<seealso cref="IWindowManager">IWindowManager</seealso>)'s <see cref="IWindowManager.ActiveWindow">ActiveWindow property</see>. 
    /// <code>
    /// // cs is assumed that <seealso cref="Poderosa.Plugins.ICoreServices">ICoreServices</seealso> is shown. 
    /// IPoderosaMainWindow mainwin = cs.WindowManager.ActiveWindow;
    /// </code>
    /// </en>
    /// </remarks>
    public interface IPoderosaMainWindow : IPoderosaForm {
        /// <summary>
        /// <ja>
        /// �r���[�}�l�[�W����Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the view manager.
        /// </en>
        /// </summary>
        IViewManager ViewManager {
            get;
        }

        /// <summary>
        /// <ja>
        /// �h�L�������g�^�u�������I�u�W�F�N�g��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the object which show the document tab.
        /// </en>
        /// </summary>
        IDocumentTabFeature DocumentTabFeature {
            get;
        }
        /// <summary>
        /// <ja>
        /// �c�[���o�[�������I�u�W�F�N�g��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the object which show the toolbar.
        /// </en>
        /// </summary>
        IToolBar ToolBar {
            get;
        }

        /// <summary>
        /// <ja>
        /// �X�e�[�^�X�o�[�������I�u�W�F�N�g��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the object which show the status bar.
        /// </en>
        /// </summary>
        IPoderosaStatusBar StatusBar {
            get;
        }

        /// <summary>
        /// <ja>
        /// �Ō�ɃA�N�e�B�u�ɂȂ����r���[��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the view that last active.
        /// </en>
        /// </summary>
        IContentReplaceableView LastActivatedView {
            get;
        }
    }

    /// <summary>
    /// <ja>
    /// �|�b�v�A�b�v�E�B���h�E�������C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface that shows pop up window.
    /// </en>
    /// </summary>
    public interface IPoderosaPopupWindow : IPoderosaForm {
        /// <summary>
        /// <ja>
        /// �|�b�v�A�b�v�E�B���h�E���̃r���[�������܂��B
        /// </ja>
        /// <en>
        /// The view in the pop up window is shown. 
        /// </en>
        /// </summary>
        IPoderosaView InternalView {
            get;
        }
        /// <summary>
        /// <ja>
        /// �X�e�[�^�X���X�V���܂��B
        /// </ja>
        /// <en>
        /// Update the status.
        /// </en>
        /// </summary>
        void UpdateStatus();
    }


    //TabBar����
    /// <summary>
    /// <ja>
    /// ���C���E�B���h�E�̃h�L�������g�^�u�������܂��B
    /// </ja>
    /// <en>
    /// The document tab in the main window is shown. 
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// ���̃C���^�[�t�F�C�X�́A<seealso cref="IPoderosaMainWindow">IPoderosaMainWindow</seealso>��
    /// <see cref="IPoderosaMainWindow.DocumentTabFeature">DocumentTabFeature�v���p�e�B</see>����
    /// �擾�ł��܂��B
    /// </ja>
    /// <en>
    /// This interface can be got from the <see cref="IPoderosaMainWindow.DocumentTabFeature">
    /// DocumentTabFeature property</see> of <seealso cref="IPoderosaMainWindow">IPoderosaMainWindow</seealso>. 
    /// </en>
    /// </remarks>
    public interface IDocumentTabFeature : IAdaptable {
        /// <summary>
        /// <ja>
        /// �h�L�������g��ǉ����܂��B
        /// </ja>
        /// <en>
        /// Add the document
        /// </en>
        /// </summary>
        /// <param name="document"><ja>�ǉ�����h�L�������g�ł��B</ja><en>Document to add.</en></param>
        void Add(IPoderosaDocument document);
        /// <summary>
        /// <ja>
        /// �h�L�������g���폜���܂��B
        /// </ja>
        /// <en>
        /// Remove the document
        /// </en>
        /// </summary>
        /// <param name="document"><ja>�폜����h�L�������g�ł��B</ja><en>Document to remove.</en></param>
        void Remove(IPoderosaDocument document);
        /// <summary>
        /// <ja>
        /// �h�L�������g���X�V���܂��B
        /// </ja>
        /// <en>
        /// Update the document
        /// </en>
        /// </summary>
        /// <param name="document"><ja>�X�V�������h�L�������g�ł��B</ja><en>Document to be update.</en></param>
        void Update(IPoderosaDocument document);
        /// <summary>
        /// <ja>
        /// �h�L�������g���A�N�e�B�u�ɂ��܂��B
        /// </ja>
        /// <en>
        /// Activate the document
        /// </en>
        /// </summary>
        /// <param name="document"><ja>�A�N�e�B�u�ɂ������h�L�������g�ł��B</ja><en>Document to active.</en></param>
        void Activate(IPoderosaDocument document);
        /// <summary>
        /// <ja>
        /// �A�N�e�B�u�ȃh�L�������g��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the active document.
        /// </en>
        /// </summary>
        IPoderosaDocument ActiveDocument {
            get;
        }

        /// <summary>
        /// <ja>
        /// �h�L�������g�̐���Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the conut of the document.
        /// </en>
        /// </summary>
        int DocumentCount {
            get;
        }
        /// <summary>
        /// <ja>
        /// �w��ʒu�̃h�L�������g��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the document at a specified position.
        /// </en>
        /// </summary>
        /// <param name="index"><ja>�擾�������h�L�������g�̃C���f�b�N�X�ʒu�ł��B</ja><en>It is an index position of the document that wants to be got. </en></param>
        /// <returns><ja>�h�L�������g������΂��̃h�L�������g���A�h�L�������g���Ȃ��ꍇ�ɂ�null���߂�܂��B</ja><en>Null returns in the document when there is no document if there is a document. </en></returns>
        IPoderosaDocument GetAtOrNull(int index);
        /// <summary>
        /// <ja>
        /// �w�肵���h�L�������g�̃C���f�b�N�X�ʒu��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the index position of the specified document.
        /// </en>
        /// </summary>
        /// <param name="document"><ja>�C���f�b�N�X�ʒu��m�肽���h�L�������g</ja><en>Document that wants to know index position</en></param>
        /// <returns><ja>�h�L�������g������΂��̃h�L�������g�ʒu�B�h�L�������g��������Ȃ��ꍇ�ɂ�-1���߂�܂��B</ja><en>It is the document position if there is a document. -1 returns when the document is not found. </en></returns>
        int IndexOf(IPoderosaDocument document);

        //�^�u�֌W
        int TabRowCount {
            get;
        }
        void SetTabRowCount(int count);

        /// <summary>
        /// Activate next view on the tab bar.
        /// </summary>
        void ActivateNextTab();

        /// <summary>
        /// Activate previous view on the tab bar.
        /// </summary>
        void ActivatePrevTab();
    }

    //StatusBar
    /// <summary>
    /// <ja>
    /// �X�e�[�^�X�o�[�������܂��B
    /// </ja>
    /// <en>
    /// The status bar is shown. 
    /// </en>
    /// </summary>
    public interface IPoderosaStatusBar {
        /// <summary>
        /// <ja>
        /// �X�e�[�^�X�o�[�̃e�L�X�g��ݒ肵�܂��B
        /// </ja>
        /// <en>
        /// The text of the status bar is set. 
        /// </en>
        /// </summary>
        /// <param name="msg"><ja>�ݒ肷��e�L�X�g</ja><en>Text to set.</en></param>
        void SetMainText(string msg);
        /// <summary>
        /// <ja>�X�e�[�^�X�o�[�̃A�C�R����ݒ肵�܂��B</ja><en>Set the icon of the status bar.</en>
        /// </summary>
        /// <param name="icon"><ja>�ݒ肷��A�C�R��</ja><en>Icon to set.</en></param>
        void SetStatusIcon(Image icon);
    }


    //Timer Suppoer
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public delegate void TimerDelegate();

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ITimerSite {
        void Close();
    }

    /// <summary>
    /// <ja>
    /// ����������܂��B
    /// </ja>
    /// <en>
    /// The language is shown. 
    /// </en>
    /// </summary>
    public enum Language {
        /// <summary>
        /// <ja>
        /// �p��</ja>
        /// <en>
        /// English</en>
        /// </summary>
        [EnumValue(Description = "Enum.Language.English")]
        English,
        /// <summary>
        /// <ja>���{��</ja>
        /// <en>Japanese</en>
        /// </summary>
        [EnumValue(Description = "Enum.Language.Japanese")]
        Japanese
    }


    /// <exclude/>
    public interface IWindowPreference {
        int WindowCount {
            get;
        }
        //Window��
        string WindowPositionAt(int index);
        string WindowSplitFormatAt(int index);
        string ToolBarFormatAt(int index);
        int TabRowCountAt(int index);
    }

    //���̃A�Z���u������Preference
    /// <summary>
    /// <ja>
    /// ���̃A�Z���u������Preference�������܂��B
    /// </ja>
    /// <en>
    /// The Preference in this assembly is shown.
    /// </en>
    /// </summary>
    /// <exclude/>
    public interface ICoreServicePreference {
        //�S�̋���
        bool ShowsToolBar {
            get;
            set;
        }
        Keys ViewSplitModifier {
            get;
            set;
        }
        int CaretInterval {
            get;
            set;
        }
        bool AutoCopyByLeftButton {
            get;
            set;
        }

        //��GUI
        int SplitLimitCount {
            get;
        }

        //���I�ύX�\����
        Language Language {
            get;
            set;
        }
    }

    //PopupView�쐬�p�����[�^
    /// <summary>
    /// <ja>
    /// PopupView���쐬����ۂ̃p�����[�^�ƂȂ�I�u�W�F�N�g�ł��B
    /// </ja>
    /// <en>
    /// Object that becomes parameter when PopupView is made.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// ���̃p�����[�^�́A<seealso cref="IWindowManager">IWindowManager</seealso>��
    /// <see cref="IWindowManager.CreatePopupView">CreatePopupView���\�b�h</see>�̈����Ƃ��Ďg���܂��B
    /// </ja>
    /// <en>
    /// This parameter is used as an argument of the <see cref="IWindowManager.CreatePopupView">CreatePopupView method</see> of <seealso cref="IWindowManager">IWindowManager</seealso>. 
    /// </en>
    /// </remarks>
    /// <exclude/>
    public class PopupViewCreationParam {
        private IViewFactory _viewFactory;
        private Size _initialSize;
        private bool _ownedByCommandTargetWindow;
        private bool _showInTaskBar;

        public PopupViewCreationParam(IViewFactory factory) {
            _viewFactory = factory;
            _initialSize = new Size(300, 300);
        }

        public IViewFactory ViewFactory {
            get {
                return _viewFactory;
            }
            set {
                _viewFactory = value;
            }
        }
        public Size InitialSize {
            get {
                return _initialSize;
            }
            set {
                _initialSize = value;
            }
        }
        public bool OwnedByCommandTargetWindow {
            get {
                return _ownedByCommandTargetWindow;
            }
            set {
                _ownedByCommandTargetWindow = value;
            }
        }
        public bool ShowInTaskBar {
            get {
                return _showInTaskBar;
            }
            set {
                _showInTaskBar = value;
            }
        }
    }


}
