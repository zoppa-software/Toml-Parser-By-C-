using System.Collections.Generic;

namespace Toml
{
    /// <summary>Toml 要素インターフェース。</summary>
    public interface ITomlValue
        : IEnumerable<KeyValuePair<string, ITomlValue>>
    {
        #region "methods"

        /// <summary>要素の種類を取得する。</summary>
        TomlValueType ValueType
        {
            get;
        }

        /// <summary>値を取得する。</summary>
        object Raw
        {
            get;
        }

        /// <summary>値をインデックスで取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        object this[int index]
        {
            get;
        }

        /// <summary>値の数を取得する。</summary>
        int Length
        {
            get;
        }

        /// <summary>指定のキーの値を取得する。</summary>
        /// <param name="key">キー。</param>
        /// <returns>値。</returns>
        ITomlValue Member(string key);

        #endregion
    }
}
