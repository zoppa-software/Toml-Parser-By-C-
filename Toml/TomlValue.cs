using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Toml
{
    /// <summary>Tomlの値を表現する。</summary>
    public static class TomlValue
    {
        #region "inner class"

        /// <summary>空ループ列挙子。</summary>
        public sealed class Noloop
            : IEnumerator<KeyValuePair<string, ITomlValue>>
        {
            /// <summary>現在の要素を取得する。</summary>
            public object Current => new KeyValuePair<string, ITomlValue>();

            /// <summary>現在の要素を取得する。</summary>
            KeyValuePair<string, ITomlValue> IEnumerator<KeyValuePair<string, ITomlValue>>.Current => new KeyValuePair<string, ITomlValue>();

            /// <summary>リソースを開放する。</summary>
            public void Dispose()
            {
                // 未使用
            }

            /// <summary>次の要素に進める。</summary>
            /// <returns>要素があれば真を返す。</returns>
            public bool MoveNext()
            {
                return false;
            }

            /// <summary>列挙子を初期値に戻す。</summary>
            public void Reset()
            {
                // 未使用
            }
        }

        #endregion

        #region "field"

        /// <summary>空の要素を取得する。</summary>
        public static ITomlValue Empty = new None();

        /// <summary>真値を取得する。</summary>
        public static ITomlValue True = new Value<bool>(true);

        /// <summary>偽値を取得する。</summary>
        public static ITomlValue False = new Value<bool>(false);

        #endregion

        #region "methods"

        /// <summary>値情報を作成する。</summary>
        /// <typeparam name="T">値の型。</typeparam>
        /// <param name="value">格納する値。</param>
        /// <returns>値情報。</returns>
        public static ITomlValue Create<T>(T value)
            where T : struct
        {
            return new Value<T>(value);
        }

        /// <summary>値情報を作成する。</summary>
        /// <param name="value">格納する値。</param>
        /// <returns>値情報。</returns>
        public static ITomlValue Create(string value)
        {
            if (!string.IsNullOrEmpty(value)) {
                return new Value<string>(value);
            }
            else if (value == "") {
                return new Value<string>("");
            }
            else {
                return TomlValue.Empty;
            }
        }

        /// <summary>配列情報を作成する。</summary>
        /// <typeparam name="T">配列の型。</typeparam>
        /// <param name="value">格納する配列。</param>
        /// <returns>配列情報。</returns>
        public static ITomlValue Create<T>(T[] value)
        {
            return (value != null ? new Array<T>(value) : TomlValue.Empty);
        }

        /// <summary>配列情報を作成する。</summary>
        /// <param name="value">格納する配列。</param>
        /// <returns>配列情報。</returns>
        public static ITomlValue Create(ITomlValue[] value)
        {
            return (value != null ? new Array<ITomlValue>(value) : TomlValue.Empty);
        }

        /// <summary>テーブル配列情報を作成する。</summary>
        /// <param name="value">格納するテーブル配列。</param>
        /// <returns>テーブル配列情報。</returns>
        public static ITomlValue Create(TomlTable[] value)
        {
            return (value != null ? new Array<TomlTable>(value) : TomlValue.Empty);
        }

        #endregion
    }

    //-------------------------------------------------------------------------
    // 空の要素
    //-------------------------------------------------------------------------
    /// <summary>空の要素を表現する。</summary>
    internal sealed class None
        : ITomlValue
    {
        #region "properties"

        /// <summary>要素の種類を取得する。</summary>
        public TomlValueType ValueType => TomlValueType.TomlNoneValue;

        /// <summary>値を取得する。</summary>
        public object Raw => null;

        /// <summary>値をインデックスで取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        public object this[int index] => throw new IndexOutOfRangeException("空値はインデックスアクセスできない");

        /// <summary>値の数を取得する。</summary>
        public int Length => 0;

        #endregion

        #region "methods"

        /// <summary>指定のキーの値を取得する。</summary>
        /// <param name="key">キー。</param>
        /// <returns>値。</returns>
        public ITomlValue Member(string key)
        {
            return TomlValue.Empty;
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<KeyValuePair<string, ITomlValue>> GetEnumerator()
        {
            return new TomlValue.Noloop();
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            return "NONE";
        }

        #endregion
    }

    //-------------------------------------------------------------------------
    // 単一値
    //-------------------------------------------------------------------------
    /// <summary>値を表現する。</summary>
    /// <typeparam name="T">値の型。</typeparam>
    internal sealed class Value<T>
        : ITomlValue
    {
        #region "field"

        /// <summary>格納値。</summary>
        private T raw;

        #endregion

        #region "properties"

        /// <summary>要素の種類を取得する。</summary>
        public TomlValueType ValueType
        {
            get {
                if (typeof(T) == typeof(bool)) {
                    return TomlValueType.TomlBooleanValue;
                }
                else if (typeof(T) == typeof(string)) {
                    return TomlValueType.TomlStringValue;
                }
                else if (typeof(T) == typeof(long)) {
                    return TomlValueType.TomlIntegerValue;
                }
                else if (typeof(T) == typeof(ulong)) {
                    return TomlValueType.TomlIntegerValue;
                }
                else if (typeof(T) == typeof(double)) {
                    return TomlValueType.TomlFloatValue;
                }
                else if (typeof(T) == typeof(TomlDate)) {
                    return TomlValueType.TomlDateValue;
                }
                else {
                    throw new InvalidOperationException("Tomlデータの型が使用できません");
                }
            }
        }

        /// <summary>値を取得する。</summary>
        public object Raw => this.raw;

        /// <summary>値をインデックスで取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        public object this[int index]
        {
            get {
                if (index != 0) {
                    return this.raw;
                }
                else {
                    throw new IndexOutOfRangeException("添え字が範囲外");
                }
            }
        }

        /// <summary>値の数を取得する。</summary>
        public int Length => 1;

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        /// <param name="raw">格納する値。</param>
        public Value(T raw)
        {
            this.raw = raw;
        }

        #endregion

        #region "methods"

        /// <summary>指定のキーの値を取得する。</summary>
        /// <param name="key">キー。</param>
        /// <returns>値。</returns>
        public ITomlValue Member(string key)
        {
            return TomlValue.Empty;
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<KeyValuePair<string, ITomlValue>> GetEnumerator()
        {
            return new TomlValue.Noloop();
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>等価判定を行う。</summary>
        /// <param name="obj">判定対象。</param>
        /// <returns>等しければ真を返す。</returns>
        public override bool Equals(object obj)
        {
            var other = obj as Value<T>;
            if (other != null) {
                return this.raw.Equals(other.raw);
            }
            else {
                throw new ArgumentException();
            }
        }

        /// <summary>ハッシュコード値を取得する。</summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode()
        {
            return this.raw.GetHashCode();
        }

        /// <summary>文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            return this.raw.ToString();
        }

        #endregion
    }

    //-------------------------------------------------------------------------
    // 配列
    //-------------------------------------------------------------------------
    /// <summary>配列を表現する。</summary>
    /// <typeparam name="T">値の型。</typeparam>
    internal sealed class Array<T>
        : ITomlValue
    {
        #region "field"

        /// <summary>格納値。</summary>
        private T[] raw;

        #endregion

        #region "properties"

        /// <summary>要素の種類を取得する。</summary>
        public TomlValueType ValueType
        {
            get {
                if (typeof(T) == typeof(TomlTable)) {
                    return TomlValueType.TomlTableArrayValue;
                }
                else if (typeof(T) == typeof(bool) ||
                         typeof(T) == typeof(string) ||
                         typeof(T) == typeof(long) ||
                         typeof(T) == typeof(double) ||
                         typeof(T) == typeof(TomlDate) ||
                         typeof(T) == typeof(ITomlValue)) {
                    return TomlValueType.TomlArrayValue;
                }
                else {
                    throw new InvalidOperationException("Tomlデータの型が使用できません");
                }
            }
        }

        /// <summary>値を取得する。</summary>
        public object Raw => this.raw.Clone();

        /// <summary>値をインデックスで取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        public object this[int index]
        {
            get {
                if (index >= 0 && index < this.raw.Length) {
                    return this.raw[index];
                }
                else {
                    throw new IndexOutOfRangeException("添え字が範囲外");
                }
            }
        }

        /// <summary>値の数を取得する。</summary>
        public int Length => this.raw.Length;

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        /// <param name="raw">格納する値。</param>
        public Array(IEnumerable<T> raw)
        {
            this.raw = raw.ToArray();
        }

        #endregion

        #region "methods"

        /// <summary>指定のキーの値を取得する。</summary>
        /// <param name="key">キー。</param>
        /// <returns>値。</returns>
        public ITomlValue Member(string key)
        {
            return TomlValue.Empty;
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<KeyValuePair<string, ITomlValue>> GetEnumerator()
        {
            return new TomlValue.Noloop();
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>等価判定を行う。</summary>
        /// <param name="obj">判定対象。</param>
        /// <returns>等しければ真を返す。</returns>
        public override bool Equals(object obj)
        {
            var other = obj as Array<T>;
            if (other != null) {
                return this.raw.Equals(other.raw);
            }
            else {
                throw new ArgumentException();
            }
        }

        /// <summary>ハッシュコード値を取得する。</summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode()
        {
            return this.raw.GetHashCode();
        }

        internal void Add(T value)
        {
            var list = new List<T>(this.raw);
            list.Add(value);
            this.raw = list.ToArray();
        }

        /// <summary>文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            var buf = new StringBuilder();
            buf.Append("[");
            if (this.Length > 0) {
                buf.Append(this.raw[0]);
                for (int i = 1; i < this.raw.Length; ++i) {
                    buf.AppendFormat(",{0}", this.raw[i]);
                }
            }
            buf.Append("]");
            return buf.ToString();
        }

        #endregion
    }
}
