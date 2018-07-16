using System;
using System.Collections.Generic;
using System.IO;

namespace Toml
{
    /// <summary>UTF8文字。</summary>
    internal struct UTF8
    {
        #region "fields"

        /// <summary>1文字。</summary>
        public byte ch1;

        /// <summary>2文字。</summary>
        public byte ch2;

        /// <summary>3文字。</summary>
        public byte ch3;

        /// <summary>4文字。</summary>
        public byte ch4;

        /// <summary>5文字。</summary>
        public byte ch5;

        /// <summary>6文字。</summary>
        public byte ch6;

        #endregion

        #region "methods"

        /// <summary>UTF8文字を作成する。</summary>
        /// <param name="buf">バイトバッファ。</param>
        /// <param name="i">変換開始位置。</param>
        /// <param name="skip">変換バイト数。</param>
        /// <returns>UTF8文字。</returns>
        internal static UTF8 Build(byte[] buf, int i, int skip)
        {
            var res = new UTF8();
            switch (skip) {
                case 1:
                    res.ch1 = buf[i];
                    break;
                case 2:
                    res.ch1 = buf[i];
                    res.ch2 = buf[i + 1];
                    break;
                case 3:
                    res.ch1 = buf[i];
                    res.ch2 = buf[i + 1];
                    res.ch3 = buf[i + 2];
                    break;
                case 4:
                    res.ch1 = buf[i];
                    res.ch2 = buf[i + 1];
                    res.ch3 = buf[i + 2];
                    res.ch4 = buf[i + 3];
                    break;
                case 5:
                    res.ch1 = buf[i];
                    res.ch2 = buf[i + 1];
                    res.ch3 = buf[i + 2];
                    res.ch4 = buf[i + 3];
                    res.ch5 = buf[i + 4];
                    break;
                default:
                    res.ch1 = buf[i];
                    res.ch2 = buf[i + 1];
                    res.ch3 = buf[i + 2];
                    res.ch4 = buf[i + 3];
                    res.ch5 = buf[i + 4];
                    res.ch6 = buf[i + 5];
                    break;
            }
            return res;
        }

        /// <summary>内部バイトをリストに展開する。</summary>
        /// <param name="buf">展開先リスト。</param>
        internal void Expand(List<byte> buf)
        {
            if (this.ch1 != 0) {
                buf.Add(this.ch1);
            }
            else {
                return;
            }

            if (this.ch2 != 0) {
                buf.Add(this.ch2);
            }
            else {
                return;
            }

            if (this.ch3 != 0) {
                buf.Add(this.ch3);
            }
            else {
                return;
            }

            if (this.ch4 != 0) {
                buf.Add(this.ch4);
            }
            else {
                return;
            }

            if (this.ch5 != 0) {
                buf.Add(this.ch5);
            }
            else {
                return;
            }

            if (this.ch6 != 0) {
                buf.Add(this.ch6);
            }
            else {
                return;
            }
        }

        #endregion
    }

    /// <summary>内部バッファクラス。</summary>
    internal sealed class TomlInnerBuffer
    {
        #region "enum"

        /// <summary>行のデータ種類。</summary>
        public enum LineType
        {
            /// <summary>空行。</summary>
            TomlLineNone,

            /// <summary>コメント行。</summary>
            TomlCommenntLine,

            /// <summary>テーブル行。</summary>
            TomlTableLine,

            /// <summary>テーブル配列行。</summary>
            TomlTableArrayLine,

            /// <summary>キー／値行。</summary>
            TomlKeyValueLine,

        }

        #endregion

        #region "inner class"

        /// <summary>文字参照イテレータ。</summary>
        public sealed class TomlIter
        {
            /// <summary>読み込みストリーム。</summary>
            private Stream sr;

            /// <summary>バッファ参照。</summary>
            private readonly TomlInnerBuffer buffer;

            /// <summary>参照インデックス。</summary>
            private int point;

            /// <summary>残文字数を取得する。</summary>
            public int RemnantLength => this.buffer.characters.Count - this.point;

            /// <summary>列位置を取得する。</summary>
            public int Xposition => this.buffer.xPosition - this.point;

            /// <summary>行位置を取得する。</summary>
            public int Yposition => this.buffer.yPosition;

            /// <summary>ストリームが読み込み済みなら真を返す。</summary>
            public bool IsEndStream => this.buffer.IsEndStream;

            /// <summary>インデックス位置を取得する。</summary>
            public int Pointer => this.point;

            /// <summary>コンストラクタ。</summary>
            /// <param name="buffer">バッファ。</param>
            /// <param name="sr">読み込みストリーム。</param>
            public TomlIter(TomlInnerBuffer buffer, Stream sr)
            {
                this.buffer = buffer;
                this.sr = sr;
                this.point = 0;
            }

            /// <summary>コンストラクタ。</summary>
            /// <param name="buffer">バッファ。</param>
            /// <param name="start">開始位置。</param>
            public TomlIter(TomlInnerBuffer buffer, int start)
            {
                this.buffer = buffer;
                this.point = start;
            }

            /// <summary>文字を取得する。</summary>
            /// <param name="index">参照位置。</param>
            public UTF8 GetChar(int index)
            {
                int i = this.point + index;
                if (i >= 0 && i < this.buffer.characters.Count) {
                    return this.buffer.characters[i];
                }
                else {
                    return new UTF8();
                }
            }

            /// <summary>指定バイト数スキップする。</summary>
            /// <param name="count">バイト数。</param>
            public void Skip(int count)
            {
                this.point += count;
            }

            /// <summary>空白文字を読み飛ばす。</summary>
            public void SkipSpace()
            {
                for (; this.point < this.buffer.characters.Count; ++this.point) {
                    if (this.buffer.characters[this.point].ch1 != '\t' &&
                        this.buffer.characters[this.point].ch1 != ' ') {
                        break;
                    }
                }
            }



            /// <summary>先頭の改行を取り除く。</summary>
            public void SkipHeadLineFeed()
            {
                if (this.buffer.characters.Count - this.point >= 2 &&
                    this.GetChar(0).ch1 == '\r' &&
                    this.GetChar(1).ch1 == '\n') {
                    this.point += 2;
                }
                else if (this.buffer.characters.Count - this.point >= 1 &&
                         this.GetChar(0).ch1 == '\n') {
                    this.point += 1;
                }
            }

            /// <summary>改行、空白部を読み飛ばす。</summary>
            public void SkipLineFeedAndSpace()
            {
                UTF8 c;
                do {
                    // 改行読み飛ばし
                    this.SkipHeadLineFeed();
                    if (this.RemnantLength <= 0) {
                        this.ReadLine();
                    }

                    // 空白部読み飛ばし
                    this.SkipSpace();

                    // 次の文字を取得
                    c = this.GetChar(0);

                    // コメントを取得したら、改行コードまで読み飛ばす
                    if (c.ch1 == '#') {
                        this.Skip(1);
                        while ((c = this.GetChar(0)).ch1 != 0) {
                            if (c.ch1 == '\n') {
                                break;
                            }
                            this.Skip(1);
                        }
                    }
                } while (c.ch1 == '\r' || c.ch1 == '\n');
            }

            /// <summary>改行まで文字を取り込む。</summary>
            public void ReadLine()
            {
                this.buffer.ReadLine(this.sr);
            }

            /// <summary>空白を読み飛ばし、次が改行／終端ならば真を返す。</summary>
            /// <returns>改行／終端ならば真を返す。</returns>
            public bool CheckLineEnd()
            {
                // 空白を読み飛ばす
                int i = this.point + 1;
                for (; i < this.buffer.characters.Count; ++i) {
                    if (this.buffer.characters[i].ch1 != '\t' &&
                        this.buffer.characters[i].ch1 != ' ') {
                        break;
                    }
                }

                // 文字を判定
                return (i >= this.buffer.characters.Count || 
                        buffer.characters[i].ch1 == '\r' ||
                        buffer.characters[i].ch1 == '\n' ||
                        buffer.characters[i].ch1 == 0);
            }

            /// <summary>空白を読み飛ばし、次が改行／終端ならば真を返す。</summary>
            /// <returns>改行／終端／コメントならば真を返す。</returns>
            public bool CheckLineEndOrComment()
            {
                // 空白を読み飛ばす
                int i = this.point;
                for (; i < this.buffer.characters.Count; ++i) {
                    if (this.buffer.characters[i].ch1 != '\t' &&
                        this.buffer.characters[i].ch1 != ' ') {
                        break;
                    }
                }

                // 文字を判定
                return (i >= this.buffer.characters.Count ||
                        this.buffer.characters[i].ch1 == '#' || 
                        this.buffer.characters[i].ch1 == '\r' ||
                        this.buffer.characters[i].ch1 == '\n' ||
                        this.buffer.characters[i].ch1 == 0);
            }
        }

        #endregion

        #region "fields"

        /// <summary>読み残しバッファ。</summary>
        private byte[] restBuffer = new byte[8];

        /// <summary>読み込み済みバイト数。</summary>
        private int readed;

        /// <summary>文字バッファ。</summary>
        readonly List<UTF8> characters;

        /// <summary>改行の次の位置。</summary>
        private int xPosition, yPosition;

        #endregion

        #region "properties"

        /// <summary>ストリームが読み込み済みなら真を返す。</summary>
        public bool IsEndStream
        {
            get;
            private set;
        }

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        public TomlInnerBuffer()
        {
            this.readed = 0;
            this.characters = new List<UTF8>();
            this.IsEndStream = false;
        }

        #endregion

        #region "method"

        /// <summary>改行まで文字を取り込む。</summary>
        /// <param name="sr">読み込みストリーム。</param>
        public bool ReadLine(Stream sr)
        {
            var res = false;
            while(true) {
                // バッファに文字を取り込む
                int maxLen = 0;
                if (this.readed > 0) {
                    maxLen = this.readed;
                    this.readed = 0;
                }
                else {
                    maxLen = sr.Read(this.restBuffer, 0, this.restBuffer.Length);
                }

                int skip = 0;
                if (maxLen > 0) {
                    res = true;
                    for (int i = 0; i < maxLen && this.restBuffer[i] != 0; i += skip) {
                        // 文字のバイト数を取得
                        skip = GetSkipByte(this.restBuffer[i]);

                        // UTF8の文字をバッファに書き込む
                        //
                        // 1. 文字数分取り込んでいるためバッファに書き込む
                        //    1-1. 改行ならば取り込み中止
                        // 2. 文字数取り込めていないため、次を取り込む
                        if (i + skip <= this.restBuffer.Length) {
                            var ch = UTF8.Build(this.restBuffer, i, skip);      // 1
                            this.characters.Add(ch);

                            if (ch.ch1 == '\n') {                               // 1-1
                                Buffer.BlockCopy(this.restBuffer, i + 1,
                                                 this.restBuffer, 0,
                                                 this.restBuffer.Length - (i + 1));
                                this.readed = maxLen - (i + 1);
                                this.xPosition = this.characters.Count;
                                this.yPosition++;
                                goto LOOP_END;
                            }
                        }
                        else {
                            Buffer.BlockCopy(this.restBuffer, i,                // 2
                                             this.restBuffer, 0, maxLen - i);
                            this.readed = maxLen - i;
                        }
                    }
                }
                else {
                    break;
                }
            }
            LOOP_END:
            this.IsEndStream = !res;
            return res;
        }

        /// <summary>UTF8文字情報を消去する。</summary>
        public void ClearUTF8()
        {
            this.characters.Clear();
        }

        /// <summary>次に進めるバイト数を取得する。</summary>
        /// <param name="caracter">判定文字。</param>
        /// <returns>進めるバイト数。</returns>
        private int GetSkipByte(byte caracter)
        {
            if ((caracter & 0xfc) == 0xfc) {
                return 6;
            }
            else if ((caracter & 0xf8) == 0xf8) {
                return 5;
            }
            else if ((caracter & 0xf0) == 0xf0) {
                return 4;
            }
            else if ((caracter & 0xe0) == 0xe0) {
                return 3;
            }
            else if ((caracter & 0xc0) == 0xc0) {
                return 2;
            }
            else {
                return 1;
            }
        }

        /// <summary>イテレータを取得する。</summary>
        /// <param name="sr">読み込みストリーム。</param>
        /// <returns>イテレータ。</returns>
        public TomlIter GetIterator(Stream sr)
        {
            return new TomlIter(this, sr);
        }

        #endregion
    }
}
