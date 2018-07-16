namespace Toml
{
    /// <summary>値の種類を表す列挙。</summary>
    public enum TomlValueType
    {
        /// <summary>空値。</summary>
        TomlNoneValue,

        /// <summary>真偽値。</summary>
        TomlBooleanValue,

        /// <summary>文字列。</summary>
        TomlStringValue,

        /// <summary>整数値。</summary>
        TomlIntegerValue,

        /// <summary>実数値。</summary>
        TomlFloatValue,

        /// <summary>日付。</summary>
        TomlDateValue,

        /// <summary>配列。</summary>
        TomlArrayValue,

        /// <summary>テーブル。</summary>
        TomlTableValue,

        /// <summary>テーブル配列。</summary>
        TomlTableArrayValue,
    }
}