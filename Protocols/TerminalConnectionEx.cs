/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: TerminalConnectionEx.cs,v 1.2 2011/10/27 23:21:57 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Poderosa.Protocols {
    /// <summary>
    /// <ja>
    /// �ʐM���邽�߂̃\�P�b�g�ƂȂ�C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface to became a  socket to connection.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>���̃C���^�[�t�F�C�X�́A�ڑ�������<seealso cref="ITerminalConnection">ITerminalConnection</seealso>��<see cref="ITerminalConnection.Socket">Socket�v���p�e�B</see>�Ƃ��Ď擾�ł��܂��B</ja><en>This interface can be got <see cref="ITerminalConnection.Socket">Socket property</see> that show connection on <seealso cref="ITerminalConnection">ITerminalConnection</seealso>.</en>
    /// </remarks>
    public interface IPoderosaSocket : IByteOutputStream {
        /// <summary>
        /// <ja>
        /// �f�[�^����M���邽�߂̃C���^�[�t�F�C�X��o�^���܂��B
        /// </ja>
        /// <en>
        /// Regist the interface to recieve data.
        /// </en>
        /// </summary>
        /// <param name="receiver"><ja>�f�[�^����M����Ƃ��ɌĂяo���C���^�[�t�F�C�X</ja><en>Interface called when recieve the data.</en></param>
        /// <remarks>
        /// <ja>
        /// ���̃��\�b�h�́A������Ăяo���āA�����̃C���^�[�t�F�C�X��o�^���邱�Ƃ͂ł��܂���B�܂��o�^�����C���^�[�t�F�C�X������������@��
        /// �p�ӂ���Ă��܂���B
        /// </ja>
        /// <en>
        /// This method cannot register a lot of interfaces by calling it two or more times. Moreover, the method of releasing the registered interface is not prepared. 
        /// </en>
        /// </remarks>
        void RepeatAsyncRead(IByteAsyncInputStream receiver);
        /// <summary>
        /// <ja>
        /// �f�[�^����M���邱�Ƃ��ł��邩�ǂ����������܂��Bfalse�̂Ƃ��ɂ̓f�[�^����M�ł��܂���B
        /// </ja>
        /// <en>
        /// It shows whether to receive the data. At false, it is not possible to receive the data. 
        /// </en>
        /// </summary>
        bool Available {
            get;
        }
        /// <summary>
        /// <ja>
        /// �ŏI�I�ȃN���[���A�b�v�����܂��B�\�P�b�gAPI�ɂ�Disconnect, Shutdown, Close��������܂�������ɂ�炸�Ɋ��S�Ȕj�������s���܂��B
        /// </ja>
        /// <en>
        /// A final cleanup is done. A complete annulment is executed without depending on it though socket API includes Disconnect, Shutdown, and Close, etc.
        /// </en>
        /// </summary>
        void ForceDisposed();
    }

    //�[���Ƃ��Ă̏o�́B��TerminalConnection�̂������̃��\�b�h�𔲂��o����
    /// <summary>
    /// <ja>
    /// �[���ŗL�̃f�[�^���o�͂���@�\��񋟂��܂��B
    /// </ja>
    /// <en>
    /// Offer the function to output peculiar data to the terminal.
    /// </en>
    /// </summary>
    public interface ITerminalOutput {
        /// <summary>
        /// <ja>
        /// �u���[�N�M���𑗐M���܂��B
        /// </ja>
        /// <en>
        /// Send break.
        /// </en>
        /// </summary>
        void SendBreak();
        /// <summary>
        /// <ja>
        /// �L�[�v�A���C�u�f�[�^�𑗐M���܂��B
        /// </ja>
        /// <en>
        /// Send keep alive data.
        /// </en>
        /// </summary>
        void SendKeepAliveData();
        /// <summary>
        /// <ja>
        /// AreYouThere�𑗐M���܂��B
        /// </ja>
        /// <en>
        /// Send AreYouThere.
        /// </en>
        /// </summary>
        void AreYouThere(); //Telnet only������
        /// <summary>
        /// <ja>
        /// �[���̃T�C�Y��ύX����R�}���h�𑗐M���܂��B
        /// </ja>
        /// <en>
        /// Send the command to which the size of the terminal is changed.
        /// </en>
        /// </summary>
        /// <param name="width"><ja>�ύX��̕��i�����P�ʁj</ja><en>Width after it changes(unit of character)</en></param>
        /// <param name="height"><ja>�ύX��̍����i�����P�ʁj</ja><en>Height after it changes(unit of character)</en></param>
        void Resize(int width, int height);
    }

	public interface ICloseableTerminalConnection : ITerminalConnection
	{
		event EventHandler ConnectionClosed;
		event ErrorEventHandler ConnectionLost;
	}

    /// <summary>
    /// <ja>
    /// �^�[�~�i���R�l�N�V�����������C���^�[�t�F�C�X�ł��B
    /// </ja>
    /// <en>
    /// Interface that show the terminal connection.
    /// </en>
    /// </summary>
    /// <remarks>
    /// <ja>
    /// ���̃C���^�[�t�F�C�X�́A<seealso cref="Poderosa.Sessions.ITerminalSession">ITerminalSession</seealso>��
    /// <see cref="Poderosa.Sessions.ITerminalSession.TerminalConnection">TerminalConnection�v���p�e�B��</see>
    /// �擾�ł��܂��B
    /// </ja>
    /// <en>
    /// This interface can be got in the <see cref="Poderosa.Sessions.ITerminalSession.TerminalConnection">TerminalConnection property</see> of <seealso cref="Poderosa.Sessions.ITerminalSession">ITerminalSession</seealso>. 
    /// </en>
    /// </remarks>
    public interface ITerminalConnection : IAdaptable {
        /// <summary>
        /// <ja>
        /// �ڑ�����������C���^�[�t�F�C�X�ł��B
        /// </ja>
        /// <en>
        /// Interface that show the connection information.
        /// </en>
        /// </summary>
        ITerminalParameter Destination {
            get;
        }
        /// <summary>
        /// <ja>
        /// �u���[�N�M���̑��M��AreYouThere�A
        /// �^�[�~�i���T�C�Y�ύX�̒ʒm�ȂǁA�^�[�~�i���ɓ��ꐧ�䂷�郁�\�b�h������ITerminalOutput�ł��B
        /// </ja>
        /// <en>
        /// It is ITerminalOutput with the method of the special control in terminals of the transmission of the break, AreYouThere, and the notification of the change of the size of the terminal, etc.
        /// </en>
        /// </summary>
        ITerminalOutput TerminalOutput {
            get;
        }
        /// <summary>
        /// <ja>
        /// �^�[�~�i���ւ̑���M�@�\������IPoderosaSocket�ł��B
        /// </ja>
        /// <en>
        /// IPoderosaSocket with transmitting and receiving function to terminal.
        /// </en>
        /// </summary>
        IPoderosaSocket Socket {
            get;
        }
        /// <summary>
        /// <ja>
        /// �ڑ������Ă��邩�ǂ����������܂��Btrue�̂Ƃ��ڑ��͕��Ă��܂��B
        /// </ja>
        /// <en>
        /// It is shown whether the connection closes. The connection close at true. 
        /// </en>
        /// </summary>
        bool IsClosed {
            get;
        }

        /// <summary>
        /// <ja>�ڑ�����܂��B</ja>
        /// <en>Close the connection.</en>
        /// </summary>
        /// <remarks>
        /// <ja>
        /// ���̃R�l�N�V�������^�[�~�i���Z�b�V�����Ƃ��Ďg���Ă���ꍇ�ɂ́A���ڂ��̃��\�b�h���Ăяo�����A
        /// �^�[�~�i���Z�b�V����������ؒf���Ă��������B
        /// </ja>
        /// <en>
        /// Please do not call this method directly when this connection is used as a terminal session, and cut it from the terminal session side. 
        /// </en>
        /// </remarks>
        void Close();

    }
}
