using System;

namespace Toml
{
    /// <summary>Toml解析例外。</summary>
    public sealed class TomlAnalisysException
        : Exception
    {
        #region "properties"

        /// <summary>列位置を取得する。</summary>
        public int XPosition
        {
            get;
        }

        /// <summary>行位置を取得する。</summary>
        private int YPosition
        {
            get;
        }

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        /// <param name="message">メッセージ。</param>
        /// <param name="iter">イテレータ。</param>
        internal TomlAnalisysException(string message,
                                       TomlInnerBuffer.TomlIter iter)
            : base(message)
        {
            this.XPosition = iter.Xposition;
            this.YPosition = iter.Yposition;
        }

        /// <summary>コンストラクタ。</summary>
        /// <param name="message">メッセージ。</param>
        /// <param name="iter">イテレータ。</param>
        /// <param name="innerException">内部例外。</param>
        internal TomlAnalisysException(string message,
                                       TomlInnerBuffer.TomlIter iter,
                                       Exception innerException)
            : base(message, innerException)
        {
            this.XPosition = iter.Xposition;
            this.YPosition = iter.Yposition;
        }

        #endregion

        #region "methods"

        /// <summary>文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            return string.Format("{0} 行:{1} 列:{2}",
                                 this.Message,
                                 (this.YPosition >= 0 ? this.YPosition : 0),
                                 (this.XPosition >= 0 ? this.XPosition : 0));
        }

        #endregion
    }
}
