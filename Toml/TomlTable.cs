using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using Toml.Properties;

namespace Toml
{
    /// <summary>Tomlのテーブルを表現する。</summary>
    public sealed class TomlTable
        : DynamicObject, ITomlValue
    {
        #region "fields"

        /// <summary>キー、値テーブル。</summary>
        private readonly Dictionary<string, ITomlValue> keyPair;

        #endregion

        #region "properties"

        /// <summary>要素の種類を取得する。</summary>
        public TomlValueType ValueType => TomlValueType.TomlTableValue;

        /// <summary>値を取得する。</summary>
        public object Raw => new Dictionary<string, ITomlValue>(this.keyPair);

        /// <summary>値をインデックスで取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        public object this[int index]
        {
            get {
                var pair = new List<KeyValuePair<string, ITomlValue>>(this.keyPair);
                if (index >= 0 && index < pair.Count) {
                    return pair[index];
                }
                else {
                    throw new IndexOutOfRangeException(Resources.INDEX_OUT_RANGE);
                }
            }
        }

        /// <summary>値の数を取得する。</summary>
        public int Length => this.keyPair.Count;

        /// <summary>定義済みテーブルならば真とする。</summary>
        internal bool IsDefined
        {
            get;
            set;
        }

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        public TomlTable()
        {
            this.keyPair = new Dictionary<string, ITomlValue>();
            this.IsDefined = false;
        }

        #endregion

        #region "member"

        /// <summary>メンバーの値を取得する。</summary>
        /// <param name="binder">オブジェクトに関する情報。</param>
        /// <param name="result">取得した値。</param>
        /// <returns>値が取得できたら真。</returns>
        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (this.keyPair.ContainsKey(binder.Name)) {
                result = this.keyPair[binder.Name];
                return true;
            }
            else {
                result = null;
                return false;
            }
        }

        /// <summary>指定のキーの値を取得する。</summary>
        /// <param name="key">キー。</param>
        /// <returns>値。</returns>
        public ITomlValue Member(string key)
        {
            ITomlValue res;
            if (this.keyPair.TryGetValue(key, out res)) {
                return res;
            }
            else {
                return TomlValue.Empty;
            }
        }

        /// <summary>指定キーが登録されていたら真を返す。</summary>
        /// <param name="keystr">キー文字列。</param>
        /// <returns>登録されていたら真。</returns>
        public bool Contains(string keystr)
        {
            return this.keyPair.ContainsKey(keystr);
        }

        /// <summary>キーと値を登録する。</summary>
        /// <param name="key">キー。</param>
        /// <param name="value">値。</param>
        public void AddKeyAndValue(string key, ITomlValue value)
        {
            if (!this.keyPair.ContainsKey(key)) {
                this.keyPair.Add(key, value);
            }
            else {
                throw new ArgumentException(Resources.REREGIST_KEY_ERR);
            }
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<KeyValuePair<string, ITomlValue>> GetEnumerator()
        {
            return this.keyPair.GetEnumerator();
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>キー、値を消去する。</summary>
        public void Clear()
        {
            this.keyPair.Clear();
        }

        /// <summary>文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            var buf = new StringBuilder();
            buf.Append("{");
            if (this.Length > 0) {
                var pair = new List<KeyValuePair<string, ITomlValue>>(this.keyPair);
                buf.AppendFormat("{0} = {1}", pair[0].Key, pair[0].Value);
                for (int i = 1; i < pair.Count; ++i) {
                    buf.AppendFormat(",{0} = {1}", pair[i].Key, pair[i].Value);
                }
            }
            buf.Append("}");
            return buf.ToString();
        }

        #endregion
    }
}
