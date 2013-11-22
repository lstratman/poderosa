/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: SSHSocket.cs,v 1.6 2011/11/19 04:58:43 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Poderosa.Util;

using Granados;
using Granados.SSH2;

namespace Poderosa.Protocols {
    //SSH�̓��o�͌n
    internal abstract class SSHConnectionEventReceiverBase : ISSHConnectionEventReceiver {
        protected SSHTerminalConnection _parent;
        protected SSHConnection _connection;
        protected IByteAsyncInputStream _callback;
        private bool _normalTerminationCalled;

        public SSHConnectionEventReceiverBase(SSHTerminalConnection parent) {
            _parent = parent;
        }
        //SSHConnection�m�����ɌĂ�
        public void SetSSHConnection(SSHConnection connection) {
            _connection = connection;
            _connection.AutoDisconnect = true; //�Ō�̃`���l���ؒf�ŃR�l�N�V�������ؒf
        }
        public SSHConnection Connection {
            get {
                return _connection;
            }
        }
        public virtual void CleanupErrorStatus() {
            if (_connection != null && _connection.IsOpen)
                _connection.Close();
        }

        public abstract void Close();

        public virtual void OnAuthenticationPrompt(string[] prompts) {
        }

        public virtual void OnConnectionClosed() {
            OnNormalTerminationCore();
            _connection.Close();
        }

        public virtual void OnError(Exception error) {
            OnAbnormalTerminationCore(error.Message);
        }

        //TODO �ő��ɂȂ����Ƃł͂��邪������E�����EXTP��
        public virtual void OnDebugMessage(bool always_display, byte[] data) {
            Debug.WriteLine(String.Format("SSH debug {0}[{1}]", data.Length, data[0]));
        }

        public virtual void OnIgnoreMessage(byte[] data) {
            Debug.WriteLine(String.Format("SSH ignore {0}[{1}]", data.Length, data[0]));
        }

        public virtual void OnUnknownMessage(byte type, byte[] data) {
            Debug.WriteLine(String.Format("Unexpected SSH packet type {0}", type));
        }

        //�ȉ��͌Ă΂�邱�Ƃ͂Ȃ��B�����
        public virtual PortForwardingCheckResult CheckPortForwardingRequest(string remote_host, int remote_port, string originator_ip, int originator_port) {
            return new Granados.PortForwardingCheckResult();
        }
        public virtual void EstablishPortforwarding(ISSHChannelEventReceiver receiver, SSHChannel channel) {
        }

	    public event EventHandler ConnectionClosed;
	    public event ErrorEventHandler ConnectionLost;

	    protected void OnNormalTerminationCore() {
            if (_normalTerminationCalled)
                return;

            /* NOTE
             *  ����I���̏ꍇ�ł��ASSH�p�P�b�g���x���ł�ChannelEOF, ChannelClose, ConnectionClose������A�ꍇ�ɂ���Ă͕������g�ݍ��킳��邱�Ƃ�����B
             *  �g�ݍ��킹�̏ڍׂ̓T�[�o�̎����ˑ��ł�����̂ŁA�����ł͂P�񂾂��K���ĂԂƂ������Ƃɂ���B
             */
            _normalTerminationCalled = true;
            EnsureCallbackHandler();
            _parent.CloseBySocket();

            try {
                if (_callback != null)
                    _callback.OnNormalTermination();
            }
            catch (Exception ex) {
                CloseError(ex);
            }

			if (ConnectionClosed != null)
				ConnectionClosed(this, new EventArgs());
        }
        protected void OnAbnormalTerminationCore(string msg) {
            EnsureCallbackHandler();
            _parent.CloseBySocket();

            try {
                if (_callback != null)
                    _callback.OnAbnormalTermination(msg);
            }
            catch (Exception ex) {
                CloseError(ex);
            }

			if (ConnectionLost != null)
				ConnectionLost(this, new ErrorEventArgs(new Exception(msg)));
        }
        protected void EnsureCallbackHandler() {
            int n = 0;
            //TODO ���ꂢ�łȂ����A�ڑ��`StartRepeat�܂ł̊ԂɃG���[���T�[�o����ʒm���ꂽ�Ƃ��ɁB
            while (_callback == null && n++ < 100) //�킸���Ȏ��ԍ��Ńn���h�����Z�b�g����Ȃ����Ƃ�����
                Thread.Sleep(100);
        }
        //Termination�����̎��s���̏���
        private void CloseError(Exception ex) {
            try {
                RuntimeUtil.ReportException(ex);
                CleanupErrorStatus();
            }
            catch (Exception ex2) {
                RuntimeUtil.ReportException(ex2);
            }
        }
    }

    internal class SSHSocket : SSHConnectionEventReceiverBase, IPoderosaSocket, ITerminalOutput, ISSHChannelEventReceiver {
        private SSHChannel _channel;
        private ByteDataFragment _data;
        private bool _waitingSendBreakReply;
        //�񓯊��Ɏ�M����B
        private MemoryStream _buffer; //RepeatAsyncRead���Ă΂��O�Ɏ�M���Ă��܂����f�[�^���ꎞ�ۊǂ���o�b�t�@

        public SSHSocket(SSHTerminalConnection parent)
            : base(parent) {
            _data = new ByteDataFragment();
        }

        public SSHChannel Channel {
            get {
                return _channel;
            }
        }

        public void RepeatAsyncRead(IByteAsyncInputStream cb) {
            _callback = cb;
            //�o�b�t�@�ɉ����������܂��Ă���ꍇ�F
            //NOTE ����́AIPoderosaSocket#StartAsyncRead���ĂԃV�[�P���X���Ȃ����A�ڑ����J�n����u��(IProtocolService�̃��\�b�h�n)����
            //�f�[�^�{�̂���M�������񋟂�����悤�ɂ���Ώ����ł���B�������v���O���}�̑��Ƃ��ẮA�ڑ��������m�F���Ă���f�[�^��M����p�ӂ������̂ŁA
            //�iPoderosa�ł����΁A���O�C���{�^����OK���������_��AbstractTerminal�܂ŏ������˂΂Ȃ�Ȃ��Ƃ������Ɓj�A������̓f�[�^��ۗ����Ă���ق����������낤
            if (_buffer != null) {
                lock (this) {
                    _buffer.Close();
                    byte[] t = _buffer.ToArray();
                    _data.Set(t, 0, t.Length);
                    if (t.Length > 0)
                        _callback.OnReception(_data);
                    _buffer = null;
                }
            }
        }

        public override void CleanupErrorStatus() {
            if (_channel != null)
                _channel.Close();
            base.CleanupErrorStatus();
        }

        public void OpenShell() {
            _channel = _connection.OpenShell(this);
        }
        public void OpenSubsystem(string subsystem) {
            SSH2Connection ssh2 = _connection as SSH2Connection;
            if (ssh2 == null)
                throw new SSHException("OpenSubsystem() can be applied to only SSH2 connection");
            _channel = ssh2.OpenSubsystem(this, subsystem);
        }

        public override void Close() {
            if (_channel != null)
                _channel.Close();
        }
        public void ForceDisposed() {
            _connection.Close(); //�}���`�`���l�����ƃA�E�g����
        }

        public void Transmit(ByteDataFragment data) {
            _channel.Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] buf, int offset, int length) {
            _channel.Transmit(buf, offset, length);
        }

        //�ȉ��AITerminalOutput
        public void Resize(int width, int height) {
            if (!_parent.IsClosed)
                _channel.ResizeTerminal(width, height, 0, 0);
        }
        public void SendBreak() {
            if (_parent.SSHLoginParameter.Method == SSHProtocol.SSH1)
                throw new NotSupportedException();
            else {
                _waitingSendBreakReply = true;
                ((Granados.SSH2.SSH2Channel)_channel).SendBreak(500);
            }
        }
        public void SendKeepAliveData() {
            if (!_parent.IsClosed) {
                // Note:
                //  Disconnecting or Closing socket may happen before Send() is called.
                //  In such case, SocketException or ObjectDisposedException will be thrown in Send().
                //  We just ignore the exceptions.
                try {
                    _connection.SendIgnorableData("keep alive");
                }
                catch (SocketException) {
                }
                catch (ObjectDisposedException) {
                }
            }
        }
        public void AreYouThere() {
            throw new NotSupportedException();
        }

        public void OnChannelClosed() {
            OnNormalTerminationCore();
        }
        public void OnChannelEOF() {
            OnNormalTerminationCore();
        }
        public void OnData(byte[] data, int offset, int length) {
            if (_callback == null) { //RepeatAsyncRead���Ă΂��O�̃f�[�^���W�߂Ă���
                lock (this) {
                    if (_buffer == null)
                        _buffer = new MemoryStream(0x100);
                    _buffer.Write(data, offset, length);
                }
            }
            else {
                _data.Set(data, offset, length);
                _callback.OnReception(_data);
            }
        }
        public void OnExtendedData(int type, byte[] data) {
        }
        public void OnMiscPacket(byte type, byte[] data, int offset, int length) {
            if (_waitingSendBreakReply) {
                _waitingSendBreakReply = false;
                if (type == (byte)Granados.SSH2.PacketType.SSH_MSG_CHANNEL_FAILURE)
                    PEnv.ActiveForm.Warning(PEnv.Strings.GetString("Message.SSHTerminalconnection.BreakError"));
            }
        }

	    public void OnChannelReady() { //!!Transmit��������ʒm���K�v�H
        }

        public void OnChannelError(Exception ex) {
            // FIXME: In this case, something message should be displayed for the user.
            //        OnAbnormalTerminationCore() doesn't show the message.
            OnAbnormalTerminationCore(ex.Message);
        }


        public SSHConnectionInfo ConnectionInfo {
            get {
                return _connection.ConnectionInfo;
            }
        }

        public bool Available {
            get {
                return _connection.Available;
            }
        }
    }

    //Keyboard Interactive�F�ؒ�
    internal class KeyboardInteractiveAuthHanlder : SSHConnectionEventReceiverBase, IPoderosaSocket {
        private MemoryStream _passwordBuffer;
        private string[] _prompts;

        public KeyboardInteractiveAuthHanlder(SSHTerminalConnection parent)
            : base(parent) {
        }

        public override void OnAuthenticationPrompt(string[] prompts) {
            //�����ɗ���P�[�X�͂Q�B

            if (_callback == null) //1. �ŏ��̔F�ؒ�
                _prompts = prompts;
            else { //2. �p�X���[�h���͂܂������Ȃǂł������Ƃ����ꍇ
                EnsureCallbackHandler();
                ShowPrompt(prompts);
            }
        }

        public void RepeatAsyncRead(IByteAsyncInputStream receiver) {
            _callback = receiver;
            if (_prompts != null)
                ShowPrompt(_prompts);
        }
        private void ShowPrompt(string[] prompts) {
            Debug.Assert(_callback != null);
            bool hasPassword = _parent.SSHLoginParameter.PasswordOrPassphrase != null
                            && !_parent.SSHLoginParameter.LetUserInputPassword;
            bool sendPassword = false;
            for (int i = 0; i < prompts.Length; i++) {
                if (hasPassword && prompts[i].Contains("assword")) {
                    sendPassword = true;
                    break;
                }
                if (i != 0)
                    prompts[i] += "\r\n";
                byte[] buf = Encoding.Default.GetBytes(prompts[i]);
                _callback.OnReception(new ByteDataFragment(buf, 0, buf.Length));
            }

            if (sendPassword) {
                SendPassword(_parent.SSHLoginParameter.PasswordOrPassphrase);
            }
        }

        public bool Available {
            get {
                return _connection.Available;
            }
        }

        public void Transmit(ByteDataFragment data) {
            Transmit(data.Buffer, data.Offset, data.Length);
        }

        public void Transmit(byte[] data, int offset, int length) {
            if (_passwordBuffer == null)
                _passwordBuffer = new MemoryStream();

            for (int i = offset; i < offset + length; i++) {
                byte b = data[i];
                if (b == 13 || b == 10) { //CR/LF
                    SendPassword(null);
                }
                else
                    _passwordBuffer.WriteByte(b);
            }
        }
        private void SendPassword(string password) {
            string[] response;
            if (password != null) {
                response = new string[] { password };
            }
            else {
                byte[] pwd = _passwordBuffer.ToArray();
                if (pwd.Length > 0) {
                    _passwordBuffer.Close();
                    _passwordBuffer.Dispose();
                    _passwordBuffer = null;
                    response = new string[] { Encoding.ASCII.GetString(pwd) };
                }
                else {
                    response = null;
                }
            }

            if (response != null) {
                _callback.OnReception(new ByteDataFragment(new byte[] { 13, 10 }, 0, 2)); //�\����CR+LF�ŉ��s���Ȃ��Ɗi�D����
                if (((Granados.SSH2.SSH2Connection)_connection).DoKeyboardInteractiveAuth(response) == AuthenticationResult.Success) {
                    _parent.SSHLoginParameter.PasswordOrPassphrase = response[0];
                    SuccessfullyExit();
                    return;
                }
            }
            _connection.Disconnect("");
            throw new IOException(PEnv.Strings.GetString("Message.SSHConnector.Cancelled"));
        }
        //�V�F�����J���A�C�x���g���V�[�o������������
        private void SuccessfullyExit() {
            SSHSocket sshsocket = new SSHSocket(_parent);
            sshsocket.SetSSHConnection(_connection);
            sshsocket.RepeatAsyncRead(_callback); //_callback�����̏����͓���
            _connection.EventReceiver = sshsocket;
            _parent.ReplaceSSHSocket(sshsocket);
            sshsocket.OpenShell();
        }

        public override void Close() {
            _connection.Close();
        }
        public void ForceDisposed() {
            _connection.Close();
        }

    }
}
