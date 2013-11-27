/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: PoderosaWorldEx.cs,v 1.3 2011/10/27 23:21:56 kzmi Exp $
 */
using System;
using System.Globalization;
using Poderosa.Boot;

namespace Poderosa.Plugins {
    /// <summary>
    /// <ja>
    /// �v���O�C������v���O�C���{�̂ƒʐM���邽�߂̃C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface to communicate from plug-in with main body of plug-in.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// ���̃C���^�[�t�F�C�X�́APoderosa�{�̂�
    /// <seealso cref="IPlugin.InitializePlugin">IPlugin.InitializePlugin</seealso>���\�b�h���Ăяo����
    /// ����������ہA�����Ƃ��ēn����܂��B<br/>
    /// �v���O�C�����ł́A���̃C���^�[�t�F�C�X�����[�J���ϐ��Ȃǂɕۑ����Ă����A�v���O�C���{�̂Ƃ̒ʐM�ɗ��p���܂��B<br/>
    /// </ja>
    /// <en>
    /// When Poderosa calls the IPlugin.InitializePlugin method and it initializes it, this interface is passed as an argument. 
    /// This interface is preserved in the local variable etc. , and it uses it to communicate with the main body of the plug-in on the plug-in side. 
    /// </en>
    /// </remarks>
    /// <ja><see href="chap01_01.html">�{�̂ƃv���O�C���̊�{�C���^�[�t�F�C�X</see></ja>
    /// <en><see href="chap01_01.html">Basic interface between Poderosa and plug-in.</see></en>
    public interface IPoderosaWorld : IAdaptable {
        /// <summary>
        /// <ja>
        /// <seealso cref="IAdapterManager">IAdapterManager�C���^�[�t�F�C�X</seealso>��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the <seealso cref="IAdapterManager">IAdapterManager interface.</seealso>
        /// </en>
        /// </summary>
        IAdapterManager AdapterManager {
            get;
        }
        /// <summary>
        /// <ja>
        /// <seealso cref="IPluginManager">IPluginManager�C���^�[�t�F�C�X</seealso>��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the <seealso cref="IPluginManager">IPluginManager interface</seealso>.
        /// </en>
        /// </summary>
        /// <remarks>
        /// <ja>
        /// <seealso cref="IPluginManager">IPluginManager�C���^�[�t�F�C�X</seealso>��Ԃ��܂��B
        /// �v���O�C�����ł́A���̃C���^�[�t�F�C�X��ʂ��āA���̃v���O�C�����񋟂���C���^�[�t�F�C�X��g���|�C���g���擾�ł��܂��B
        /// </ja>
        /// <en>
        /// Return the <seealso cref="IPluginManager">IPluginManager interface</seealso>��Ԃ��܂��B
        /// The interface and the extension point that other plug-ins offer through this interface can be acquired on the plug-in side. 
        /// </en>
        /// </remarks>
        /// <ja><see href="chap02.html">Poderosa�{�̂ƃv���O�C���Ƃ̂��Ƃ�</see></ja>
        /// <en><see href="chap02.html">Communication of Poderosa and plug-in</see></en>
        IPluginManager PluginManager {
            get;
        }
        /// <summary>
        /// <ja>
        /// <seealso cref="IPoderosaCulture">IPoderosaCulture</seealso>�C���^�[�t�F�C�X��Ԃ��܂��B
        /// </ja>
        /// <en>
        /// Return the <seealso cref="IPoderosaCulture">IPoderosaCulture</seealso> interface.
        /// </en>
        /// </summary>
        IPoderosaCulture Culture {
            get;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IPoderosaApplication : IAdaptable {
        string HomeDirectory {
            get;
        }
        string ProfileHomeDirectory {
            get;
        }
        IPoderosaLog PoderosaLog {
            get;
        }
        string[] CommandLineArgs {
            get;
        }
        IPoderosaWorld Start();
		IPoderosaWorld Start(ITracer tracer);
        void Shutdown();
        string InitialOpenFile {
            get;
        } //���w���null
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IStartupContextSupplier : IAdaptable {
        StructuredText Preferences {
            get;
        }
        string PreferenceFileName {
            get;
        } //preference�͏�Ƀt�@�C������ǂނƂ͌���Ȃ��Bnull�̂��Ƃ�����
    }

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface IPoderosaCulture {
        CultureInfo InitialCulture {
            get;
        }
        CultureInfo CurrentCulture {
            get;
        }
        void SetCulture(CultureInfo culture);
        void AddChangeListener(ICultureChangeListener listener);
        void RemoveChangeListener(ICultureChangeListener listener);

        //OS�����{�ꂩ�ǂ���
        bool IsJapaneseOS {
            get;
        }
        bool IsSimplifiedChineseOS {
            get;
        }
        bool IsTraditionalChineseOS {
            get;
        }
        bool IsKoreanOS {
            get;
        }
    }

    //����ύX�ʒm

    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ICultureChangeListener {
        void OnCultureChanged(CultureInfo newculture);
    }
}
