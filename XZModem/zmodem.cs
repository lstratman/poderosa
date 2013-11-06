/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: zmodem.cs,v 1.2 2011/10/27 23:21:59 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Diagnostics;

using Poderosa.Terminal;
using Poderosa.Protocols;

namespace Poderosa.XZModem {
    internal abstract class ZModem : ModemBase {
        // zmodem packet type(1byte)
        protected const byte ZPAD = (byte)'*';
        protected const byte ZDLE = 0x18;
        protected const byte ZDLEE = 0x58;
        protected const byte ZBIN = (byte)'A';
        protected const byte ZHEX = (byte)'B';
        protected const byte ZBIN32 = (byte)'C';

        protected const byte ZRQINIT = 0;
        protected const byte ZRINIT = 1;
        protected const byte ZSINIT = 2;
        protected const byte ZACK = 3;
        protected const byte ZFILE = 4;
        protected const byte ZSKIP = 5;
        protected const byte ZNAK = 6;
        protected const byte ZABORT = 7;
        protected const byte ZFIN = 8;
        protected const byte ZRPOS = 9;
        protected const byte ZDATA = 10;
        protected const byte ZEOF = 11;
        protected const byte ZFERR = 12;
        protected const byte ZCRC = 13;
        protected const byte ZCHALLENGE = 14;
        protected const byte ZCOMPL = 15;
        protected const byte ZCAN = 16;
        protected const byte ZFREECNT = 17;
        protected const byte ZCOMMAND = 18;
        protected const byte ZSTDERR = 19;

        protected const byte ZCRCE = (byte)'h';
        protected const byte ZCRCG = (byte)'i';
        protected const byte ZCRCQ = (byte)'j';
        protected const byte ZCRCW = (byte)'k';
        protected const byte ZRUB0 = (byte)'l';
        protected const byte ZRUB1 = (byte)'m';

        protected const byte ZF0 = 3;
        protected const byte ZF1 = 2;
        protected const byte ZF2 = 1;
        protected const byte ZF3 = 0;
        protected const byte ZP0 = 0;
        protected const byte ZP1 = 1;
        protected const byte ZP2 = 2;
        protected const byte ZP3 = 3;

        protected const byte CANFDX = 0x01;
        protected const byte CANOVIO = 0x02;
        protected const byte CANBRK = 0x04;
        protected const byte CANCRY = 0x08;
        protected const byte CANLZW = 0x10;
        protected const byte CANFC32 = 0x20;
        protected const byte ESCCTL = 0x40;
        protected const byte ESC8 = 0x80;

        protected const byte ZCBIN = 1;
        protected const byte ZCNL = 2;

        protected const byte CR = 0x8D;
        protected const byte LF = 0x8A;
        protected const byte XON = 0x11;
        protected const byte XOFF = 0x13;

        protected string _filename;
        protected Timer _timer;

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public enum State {
            // for sending
            WaitingZPAD,
            WaitingZDLE,
            GetHeaderFormat,
            GetBinaryData,
            GetHexData,
            GetHexEOL,

            // for receiving
            GetFileInfo,
            GetFileData,
        }

        /// <summary>
        /// 
        /// </summary>
        /// <exclude/>
        public enum FileDirectionState {
            SendFile,
            ReceiveFile,
        }

        protected XZModemDialog _parent;
        protected State _state;
        protected byte[] _PktIn = new byte[1032];
        protected int _PktInCount;
        protected int _PktInIndex;
        protected bool _HexLo;
        protected int _RxType;
        protected byte[] _RxHdr = new byte[4];
        protected int _CurrentPos, _LastPos;
        protected bool _abort;

        protected FileStream _filestream;
        protected int _filesize;

        public ZModem(XZModemDialog parent, string filename) {
            _parent = parent;
            _filename = filename;
            _abort = false;
        }

        // CRC�̌v�Z
        public ushort UpdateCRC(byte b, ushort crc) {
            int i;

            crc = (ushort)(crc ^ (ushort)(b << 8));
            for (i = 1; i <= 8; i++)
                if ((crc & 0x8000) != 0)
                    crc = (ushort)((crc << 1) ^ 0x1021);
                else
                    crc = (ushort)(crc << 1);
            return crc;
        }

        // HEX�`���ŏ�������
        public int PutHex(byte[] data, int index, byte b) {
            // b => ['0'-'9'|'a'-'f']['0'-'9'|'a'-'f']
            if (b <= 0x9f) {
                data[index] = (byte)((b >> 4) + 0x30);
            }
            else {
                data[index] = (byte)((b >> 4) + 0x57);
            }
            index++;
            if ((b & 0x0F) <= 0x09) {
                data[index] = (byte)((b & 0x0F) + 0x30);
            }
            else {
                data[index] = (byte)((b & 0x0F) + 0x57);
            }
            index++;

            return (index);
        }

        // �o�C�i���`���ŏ�������
        public int PutBin(byte[] data, int index, byte b) {
            switch (b) {
                case 0x0D:
                case 0x8D:
                case 0x10:
                case 0x11:
                case 0x13:
                case ZDLE:
                case 0x90:
                case 0x91:
                case 0x93:
                    data[index++] = ZDLE;
                    b ^= 0x40;
                    break;

                default:
                    break;
            }
            data[index++] = b;

            return (index);
        }

        public ushort LOWORD(int pos) {
            return (ushort)(pos & 0xffff);
        }

        public ushort HIWORD(int pos) {
            return (ushort)(pos >> 16);
        }

        public byte LOBYTE(ushort pos) {
            return (byte)(pos & 0xff);
        }

        public byte HIBYTE(ushort pos) {
            return (byte)(pos >> 8);
        }

        // �T�[�o����̎�M�f�[�^����͂���
        // Reader�N���X����Ă΂��
        public override void OnReception(ByteDataFragment fragment) {
            byte[] data = fragment.Buffer;
            int offset = fragment.Offset;
            int length = fragment.Length;
            int i;
            byte c;

            Debug.WriteLine(String.Format("OnReception len={0} state={1} pos={2}", length, _state.ToString(), _CurrentPos));

            if (_state == State.GetFileInfo) {
                // �t�@�C�������擾����
                _state = State.WaitingZPAD;
                ParseFileInfo(data, offset, length);
                return;

            }
            else if (_state == State.GetFileData) {
                // �t�@�C���̓��e��ǂݎ��A�������ށB
                int p = ParseFileData(data, offset, length);
                offset += p;
                length -= p;
            }

            for (i = 0; i < length; i++) {
                c = data[offset + i];

                // 0x11, 0x13, 0x81, 0x83�͖�������
                if ((c & 0x7f) == XON || (c & 0x7f) == XOFF)
                    continue;

                if (_state == State.WaitingZPAD) {
                    switch (c) {
                        case ZPAD:
                            _state = State.WaitingZDLE;
                            break;
                        default:
                            break;
                    }

                }
                else if (_state == State.WaitingZDLE) {
                    switch (c) {
                        case ZPAD:
                            break;
                        case ZDLE:
                            _state = State.GetHeaderFormat;
                            break;
                        default:
                            _state = State.WaitingZPAD;
                            break;
                    }

                }
                else if (_state == State.GetHeaderFormat) {
                    switch (c) {
                        case ZBIN:
                            _PktInCount = 7;
                            _state = State.GetBinaryData;
                            break;

                        case ZHEX:
                            _PktInCount = 7;
                            _state = State.GetHexData;
                            break;

                        case ZBIN32:
                            _PktInCount = 9;
                            _state = State.GetBinaryData;
                            break;

                        default:
                            _state = State.WaitingZPAD;
                            break;
                    }
                    // initialize variables
                    _PktInIndex = 0;
                    _HexLo = false;

                }
                else if (_state == State.GetBinaryData)  // binary('A') data
                {
                    _PktIn[_PktInIndex++] = c;
                    _PktInCount--;
                    if (_PktInCount <= 0) {
                        _state = State.WaitingZPAD;

                        if (CheckHeader(_PktIn, _PktInCount)) {
                            ParseHeader();

                            if (_RxType == ZDATA)
                                i += ParseFileData(data, offset + 10, length - 10);
                            else if (_RxType == ZFILE)
                                ParseFileInfo(data, offset + 10, length - 10);
                        }
                    }

                }
                else if (_state == State.GetHexData)  // HEX('B') data
                {
                    if (c <= '9') {
                        c -= 0x30;
                    }
                    else if ((c >= 'a') && (c <= 'f')) {
                        c -= 0x57;
                    }
                    else {
                        _state = State.WaitingZPAD;
                        break;  // for loop
                    }

                    if (_HexLo) {  // lower
                        _PktIn[_PktInIndex] |= c;
                        _PktInIndex++;
                        _HexLo = false;
                        _PktInCount--;

                        if (_PktInCount <= 0) {
                            _state = State.GetHexEOL;
                            _PktInCount = 2;  // CR��LF�̕����X�L�b�v����
                        }
                    }
                    else {  // upper
                        _PktIn[_PktInIndex] = (byte)(c << 4);
                        _HexLo = true;
                    }
                }
                else if (_state == State.GetHexEOL) {
                    _PktInCount--;
                    if (_PktInCount <= 0) {
                        _state = State.WaitingZPAD;

                        if (CheckHeader(_PktIn, _PktInCount)) {
                            ParseHeader();
                        }
                    }
                }
            }

        }


        // �p�P�b�g�w�b�_��CRC�`�F�b�N
        private bool CheckHeader(byte[] data, int len) {
            ushort crc;
            int i;

            crc = 0;
            for (i = 0; i < 7; i++) {
                crc = UpdateCRC(data[i], crc);
            }

            //�����t�@�C������M����Ƃ��ACRC���O�łȂ��B�����炭�f�[�^�̓r���œ���CRC�𖳎����Ă���̂����R�Ȃ̂��낤���A�ʓ|�Ȃ̂ŃX�L�b�v�B�f�[�^��M�������͐������ɃX���[����B
            if (crc == 0 || (_filesize > 0 && _CurrentPos == _filesize)) { // CRC is OK.
                _RxType = data[0];  // packet type
                for (i = 1; i <= 4; i++) {
                    _RxHdr[i - 1] = data[i];
                }
                return true;
            }
            else {
                // TODO:

                return false;
            }
        }

        // ZMODEM�p�P�b�g����
        public abstract void ParseHeader();


        // ZFILE�̒���ɑ����Ă���t�@�C�����ƃT�C�Y�𓾂�
        private void ParseFileInfo(byte[] data, int offset, int length) {
            byte[] filename = new byte[1024];
            byte c;
            string fname;
            int i, n;
            int size;

            n = 0;
            for (i = 0; i < length; i++) {
                c = data[offset + i];
                if (c != 0x00) {
                    filename[n++] = c;
                }
                else {
                    break;
                }
            }
            fname = Encoding.ASCII.GetString(filename, 0, n);

            size = 0;
            for (i = n + 1; i < length; i++) {
                c = data[offset + i];
                if (c >= 0x30 && c <= 0x39)  // '0' - '9'
                {
                    size = size * 10 + (c - 0x30);
                }
                else {
                    break;
                }
            }

            // setting up
            _filesize = size;
            _CurrentPos = 0;

        }

        // ZDATA�̒���ɑ����Ă���t�@�C���f�[�^
        private int ParseFileData(byte[] data, int offset, int length) {
            byte c;
            bool escaping = false;

            for (int i = 0; i < length; i++) {
                c = data[offset + i];

                if (_CurrentPos == _filesize) {
                    _state = State.WaitingZPAD;
                    return i + 4; //CRC�X�L�b�v
                }

                if (c == ZDLE) {
                    //CRC�}�����m�ŃX�L�b�v
                    byte next = data[offset + i + 1];
                    if (next == ZCRCG || next == ZCRCE || next == ZCRCQ || next == ZCRCW) { //�ǂ�CRC������̂��s���A�܂�CRC�̐������`�F�b�N�͂��ڂ�
                        i += 3; //ZDLE�܂߂ĂS�o�C�g��΂�
                    }
                    else
                        escaping = true; //�P�Ȃ�G�X�P�[�v
                }
                else {
                    if (escaping) {
                        c ^= 0x40;
                        escaping = false;
                    }
                    _filestream.WriteByte(c);
                    _CurrentPos++;
                    if ((_CurrentPos % 1000) == 0)
                        _parent.AsyncSetProgressValue((int)_CurrentPos);
                }
            }

            return length;
        }



        public override void Abort() {
            Fail(null);
        }

        public override void Dispose() {
            if (_timer != null)
                _timer.Dispose();
            if (_filestream != null) {
                try {
                    _filestream.Close();
                }
                catch (Exception ex) {
                    RuntimeUtil.SilentReportException(ex);
                }
            }
        }
        protected void Fail(string msg) {
            _parent.AsyncClose();
            _site.Cancel(msg);
            Dispose();
        }

        private byte[] _byteBuf = new byte[1];
        protected void SendByte(byte b) {
            _byteBuf[0] = b;
            _connection.Socket.Transmit(_byteBuf, 0, 1);
        }

        protected void Complete() {
            _site.Complete();
            _parent.AsyncClose();
            Dispose();
        }

        #region IModalTerminalTask
        public override string Caption {
            get {
                return "ZMODEM";
            }
        }

        #endregion

    }


    // zmodem sending file
    internal class ZModemSender : ZModem {
        private byte[] _body;
        private const int NEGOTIATION_TIMEOUT = 1;
        private byte[] _TxHdr = new byte[4];
        private byte[] _PktOut = new byte[1032];
        private int _PktOutCount;
        private int _MaxDataLen = 1024;
        private Thread _sendThread;

        private enum SendState {
            Sending,  // ���M��
            EOF,      // ���M����
        }

        public ZModemSender(XZModemDialog parent, string filename)
            : base(parent, filename) {
            _filesize = (int)new FileInfo(filename).Length;
            _body = new byte[_filesize];
            FileStream strm = new FileStream(filename, FileMode.Open, FileAccess.Read);
            strm.Read(_body, 0, _body.Length);
            strm.Close();
        }
        public override bool IsReceivingTask {
            get {
                return false;
            }
        }

        public override void Start() {
            // TODO: �^�C�}����
            _timer = new Timer(new TimerCallback(OnTimeout), NEGOTIATION_TIMEOUT, 60000, Timeout.Infinite);

            _state = State.WaitingZPAD;
            SendZRQInit();  // ZQRINIT(0)
        }

        private void OnTimeout(object state) {
            _timer.Dispose();
            _timer = null;
            switch ((int)state) {
                case NEGOTIATION_TIMEOUT:
                    Abort();
                    break;
                default:
                    break;
            }

        }

        public override void Abort() {
            if (_sendThread != null) {
                _sendThread.Abort();
            }
            // ZFIN�𑗐M���A�V�[�P���X�𒆒f������B
            SendZFIN();
            base.Abort();
        }

        // �p�P�b�g���M
        private void SendPacket(byte[] data, int len) {
            _connection.Socket.Transmit(data, 0, len);

#if false
            Console.WriteLine(Encoding.ASCII.GetString(data, 0, len));
            for (int i = 0; i < len; i++) {
                byte b = data[i];
                System.Console.Write("{0:x2} ", b);
                if (b == 0x0a) {
                    System.Console.WriteLine("");
                }
            }
            System.Console.WriteLine(">>>=========to server===============>>>");
#endif
        }

        // ���M�w�b�_
        private void BuildXmitHeader(int pos) {
            _TxHdr[ZP0] = LOBYTE(LOWORD(pos));
            _TxHdr[ZP1] = HIBYTE(LOWORD(pos));
            _TxHdr[ZP2] = LOBYTE(HIWORD(pos));
            _TxHdr[ZP3] = HIBYTE(HIWORD(pos));
        }

        // ���M�pHEX�w�b�_�p�P�b�g�����
        private int BuildSendHEXHeader(byte[] data, byte hdr_type, int data_index) {
            int index, i;
            ushort crc;

            data[0] = ZPAD;
            data[1] = ZPAD;
            data[2] = ZDLE;
            data[3] = ZHEX;
            index = 4;
            index = PutHex(data, index, hdr_type);

            crc = UpdateCRC(hdr_type, 0);
            for (i = 0; i < 4; i++) {
                index = PutHex(data, index, _TxHdr[i]);
                crc = UpdateCRC(_TxHdr[i], crc);
            }
            index = PutHex(data, index, HIBYTE(crc));
            index = PutHex(data, index, LOBYTE(crc));
            data[index++] = CR;
            data[index++] = LF;

            if (!(hdr_type == ZFIN || hdr_type == ZACK)) {
                data[index++] = XON;
            }
            data_index = index;

            return (data_index);
        }

        // ���M�p�o�C�i���w�b�_�p�P�b�g�����
        private int BuildSendBinaryHeader(byte[] data, byte hdr_type, int data_index) {
            int index, i;
            ushort crc;

            data[0] = ZPAD;
            data[1] = ZDLE;
            data[2] = ZBIN;
            index = 3;
            index = PutBin(data, index, hdr_type);

            crc = UpdateCRC(hdr_type, 0);
            for (i = 0; i < 4; i++) {
                index = PutBin(data, index, _TxHdr[i]);
                crc = UpdateCRC(_TxHdr[i], crc);
            }
            index = PutBin(data, index, HIBYTE(crc));
            index = PutBin(data, index, LOBYTE(crc));

            data_index = index;

            return (data_index);
        }

        // ZRQINIT
        private void SendZRQInit() {
            BuildXmitHeader(0);
            _PktOutCount = BuildSendHEXHeader(_PktOut, ZRQINIT, _PktOutCount);
            SendPacket(_PktOut, _PktOutCount);
        }

        // ZFILE(step1)
        private void SendZFILE() {
            int max;

            max = _RxHdr[ZP1] << 8 | _RxHdr[ZP0];
            if (max <= 0)
                max = 1024;
            if (_MaxDataLen > max)
                _MaxDataLen = max;

            // �t�@�C���w�b�_�̑��M
            BuildXmitHeader(0);
            _TxHdr[ZF0] = ZCBIN;  // binary file
            _PktOutCount = BuildSendBinaryHeader(_PktOut, ZFILE, _PktOutCount);  // ZFILE(4)
            SendPacket(_PktOut, _PktOutCount);

        }

        // ZFILE(step2)
        private void SendZFILEContent() {
            ushort crc;
            int i;

            // �t�@�C���f�[�^�̑��M
            string fn = _filename;
            int pathchar = fn.LastIndexOf('\\');
            if (pathchar != -1)
                fn = fn.Substring(pathchar + 1);
            fn = fn.Replace(" ", "_");  // �󔒂�_�֑S�u��
            byte[] b = Encoding.ASCII.GetBytes(fn);
            Array.Copy(b, 0, _PktOut, 0, b.Length);
            _PktOutCount = b.Length;
            crc = 0;
            for (i = 0; i < _PktOutCount; i++) {
                crc = UpdateCRC(_PktOut[i], crc);
            }
            _PktOutCount = PutBin(_PktOut, _PktOutCount, 0);  // null terminate
            crc = UpdateCRC(0, crc);

            b = Encoding.ASCII.GetBytes(_filesize.ToString());
            Array.Copy(b, 0, _PktOut, _PktOutCount, b.Length);
            for (i = 0; i < b.Length; i++) {
                crc = UpdateCRC(_PktOut[_PktOutCount], crc);
                _PktOutCount++;
            }
            _PktOutCount = PutBin(_PktOut, _PktOutCount, 0);  // null terminate
            crc = UpdateCRC(0, crc);

            _PktOut[_PktOutCount++] = ZDLE;
            _PktOut[_PktOutCount++] = ZCRCW;
            crc = UpdateCRC(ZCRCW, crc);

            _PktOutCount = PutBin(_PktOut, _PktOutCount, HIBYTE(crc));
            _PktOutCount = PutBin(_PktOut, _PktOutCount, LOBYTE(crc));

            SendPacket(_PktOut, _PktOutCount);
        }

        // ZDATA(10)
        private void SendZDATA() {
            // �t�@�C���w�b�_�̑��M
            BuildXmitHeader(_CurrentPos);
            _PktOutCount = BuildSendBinaryHeader(_PktOut, ZDATA, _PktOutCount);
            SendPacket(_PktOut, _PktOutCount);
        }

        private SendState SendZDATAContent() {

            // �t�@�C���f�[�^�̑��M
            if (_CurrentPos >= _filesize) { // �]���I��
                _CurrentPos = _filesize;

                // ZEOF(11)�̑��M
                BuildXmitHeader(_CurrentPos);
                _PktOutCount = BuildSendHEXHeader(_PktOut, ZEOF, _PktOutCount);
                SendPacket(_PktOut, _PktOutCount);
                return SendState.EOF;
            }

            ushort crc = 0;
            int bytecount = _CurrentPos;
            byte b;
            _PktOutCount = 0;
            for (int i = _CurrentPos; i < _filesize; i++) {
                b = _body[i];

                _PktOutCount = PutBin(_PktOut, _PktOutCount, b);
                crc = UpdateCRC(b, crc);
                bytecount++;

                // ��񓖂���̑��M�T�C�Y��1KB�����ɗ}����
                if (_PktOutCount > _MaxDataLen - 2)
                    break;
            }
            _CurrentPos = bytecount;  // update current position(file offset)

            _PktOut[_PktOutCount++] = ZDLE;
            if (_CurrentPos >= _filesize) {
                b = ZCRCE;
            }
            else {
                // TODO: window size
                b = ZCRCG;
            }

            _PktOut[_PktOutCount++] = b;
            crc = UpdateCRC(b, crc);

            _PktOutCount = PutBin(_PktOut, _PktOutCount, HIBYTE(crc));
            _PktOutCount = PutBin(_PktOut, _PktOutCount, LOBYTE(crc));

            SendPacket(_PktOut, _PktOutCount);

            return SendState.Sending;
        }

        // ZFIN
        private void SendZFIN() {
            BuildXmitHeader(0);
            _PktOutCount = BuildSendHEXHeader(_PktOut, ZFIN, _PktOutCount);
            SendPacket(_PktOut, _PktOutCount);
        }

        // NULL
        private void SendNULL() {
            _PktOut[0] = (byte)'0';
            _PktOut[1] = (byte)'0';
            _PktOutCount = 2;
            SendPacket(_PktOut, _PktOutCount);
        }

        private void StartSendThread() {
            _sendThread = new Thread(new ThreadStart(SendThreadEntryPoint));
            _sendThread.Start();
        }
        private void SendThreadEntryPoint() {
            try {
                //ZEOF�̑��M�܂ň�C�ɁB
                while (SendZDATAContent() == SendState.Sending) {
                    ;
                }
            }
            catch (Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
            finally {
                _sendThread = null;
            }
        }

        public override void ParseHeader() {
            switch (_RxType) {

                case ZRINIT:
                    if (_CurrentPos == 0) {
                        SendZFILE();  // ZFILE
                        SendZFILEContent();
                    }
                    else
                        SendZFIN(); //�I������
                    break;

                case ZRPOS:  // ���M�J�n�ʒu
                    int pos = _RxHdr[ZP3];
                    pos = (pos << 8) | _RxHdr[ZP2];
                    pos = (pos << 8) | _RxHdr[ZP1];
                    pos = (pos << 8) | _RxHdr[ZP0];
                    _CurrentPos = pos;
                    _LastPos = pos;
                    //ZRPOS�҂��đ��M
                    SendZDATA();
                    StartSendThread();
                    break;

                case ZFIN:
                    SendNULL();
                    _site.MainWindow.Information(XZModemPlugin.Instance.Strings.GetString("Message.XModem.SendComplete"));
                    Complete();
                    break;

                case ZSKIP:  // ���łɓ����t�@�C��������
                    _abort = true;
                    Fail("ZSKIP (maybe the host detects file name confliction)");
                    break;

                default:
                    Fail(String.Format("ZMODEM Unexpected response '{0}'", _RxType));
                    break;
            }
        }
    }


    // zmodem receiving file
    internal class ZModemReceiver : ZModem {
        private const int NEGOTIATION_TIMEOUT = 1;
        private byte[] _TxHdr = new byte[4];
        private byte[] _PktOut = new byte[1032];
        private int _PktOutCount;

        public ZModemReceiver(XZModemDialog dlg, string filename)
            : base(dlg, filename) {
        }

        public override void Start() {
            // 10sec. ZRINIT sending timer
            _timer = new Timer(new TimerCallback(OnTimeout), NEGOTIATION_TIMEOUT, 10000, Timeout.Infinite);
            _filestream = new FileStream(_filename, FileMode.Create); //�_�C�A���O�Ŏw�肵���t�@�C�����g�p
            _state = State.WaitingZPAD;
            //NOTE Enter�������̂������Ŏ���������΃E�}�������Ɗ��҂��Ă������A����Ă���f�[�^�T�C�Y���炵�ĈႤ�B�v���g�R���I�v�V�����n�ŉ����s����������悤�Ȉ�ۂ����AZMODEM�ɂ����܂Ŋ撣��Ȃ��̂ł�����߂�
            //_site.SendEnter();
        }

        public override void Abort() {
            _PktOutCount = BuildSendHEXHeader(_PktOut, ZABORT, 0);
            SendPacket(_PktOut, _PktOutCount);
            base.Abort();
        }

        public override bool IsReceivingTask {
            get {
                return true;
            }
        }

        private void OnTimeout(object state) {
            _timer.Dispose();
            _timer = null;
            switch ((int)state) {
                case NEGOTIATION_TIMEOUT:
                    SendZRInit();
                    break;
                default:
                    break;
            }

        }

        // �p�P�b�g���M
        private void SendPacket(byte[] data, int len) {
            _connection.Socket.Transmit(data, 0, len);

#if false
            Console.WriteLine(Encoding.ASCII.GetString(data, 0, len));
            for (int i = 0; i < len; i++)
            {
                byte b = data[i];
                System.Console.Write("{0:x2} ", b);
                if (b == 0x0a)
                {
                    System.Console.WriteLine("");
                }
            }
            System.Console.WriteLine(">>>=========to server===============>>>");
#endif
        }

        // ���M�w�b�_
        private void BuildXmitHeader(int pos) {
            _TxHdr[ZP0] = LOBYTE(LOWORD(pos));
            _TxHdr[ZP1] = HIBYTE(LOWORD(pos));
            _TxHdr[ZP2] = LOBYTE(HIWORD(pos));
            _TxHdr[ZP3] = HIBYTE(HIWORD(pos));
        }

        // ���M�pHEX�w�b�_�p�P�b�g�����
        private int BuildSendHEXHeader(byte[] data, byte hdr_type, int data_index) {
            int index, i;
            ushort crc;

            data[0] = ZPAD;
            data[1] = ZPAD;
            data[2] = ZDLE;
            data[3] = ZHEX;
            index = 4;
            index = PutHex(data, index, hdr_type);

            crc = UpdateCRC(hdr_type, 0);
            for (i = 0; i < 4; i++) {
                index = PutHex(data, index, _TxHdr[i]);
                crc = UpdateCRC(_TxHdr[i], crc);
            }
            index = PutHex(data, index, HIBYTE(crc));
            index = PutHex(data, index, LOBYTE(crc));
            data[index++] = CR;
            data[index++] = LF;

            if (!(hdr_type == ZFIN || hdr_type == ZACK)) {
                data[index++] = XON;
            }
            data_index = index;

            return (data_index);
        }

        // ���M�p�o�C�i���w�b�_�p�P�b�g�����
        private int BuildSendBinaryHeader(byte[] data, byte hdr_type, int data_index) {
            int index, i;
            ushort crc;

            data[0] = ZPAD;
            data[1] = ZDLE;
            data[2] = ZBIN;
            index = 3;
            index = PutBin(data, index, hdr_type);

            crc = UpdateCRC(hdr_type, 0);
            for (i = 0; i < 4; i++) {
                index = PutBin(data, index, _TxHdr[i]);
                crc = UpdateCRC(_TxHdr[i], crc);
            }
            index = PutBin(data, index, HIBYTE(crc));
            index = PutBin(data, index, LOBYTE(crc));

            data_index = index;

            return (data_index);
        }

        // ZRINIT
        private void SendZRInit() {
            BuildXmitHeader(0);
            _TxHdr[ZF0] = CANFDX | CANOVIO;
            _PktOutCount = BuildSendHEXHeader(_PktOut, ZRINIT, _PktOutCount);
            SendPacket(_PktOut, _PktOutCount);
        }

        // ZRPOS
        private void SendZRPOS() {
            BuildXmitHeader(_CurrentPos);
            _PktOutCount = BuildSendHEXHeader(_PktOut, ZRPOS, _PktOutCount);
            SendPacket(_PktOut, _PktOutCount);
        }
        // ZFIN
        private void SendZFIN() {
            BuildXmitHeader(0);
            _PktOutCount = BuildSendHEXHeader(_PktOut, ZFIN, _PktOutCount);
            SendPacket(_PktOut, _PktOutCount);
        }

        public override void ParseHeader() {
            Debug.WriteLine("PH " + _RxType);
            switch (_RxType) {
                case ZRQINIT:
                    SendZRInit();
                    break;

                case ZFIN:
                    SendZFIN();
                    _site.MainWindow.Information(XZModemPlugin.Instance.Strings.GetString("Message.XModem.ReceiveComplete"));
                    Complete();
                    break;

                case ZFILE:
                    _CurrentPos = 0;
                    SendZRPOS(); // ZRPOS
                    break;

                case ZDATA:
                    _state = State.GetFileData; // ���̓t�@�C���̓��e���擾����
                    break;

                case ZEOF:
                    SendZRInit();
                    break;

                default:
                    break;
            }
        }
    }


}
