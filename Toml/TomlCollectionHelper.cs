using Toml.Properties;

namespace Toml
{
    /// <summary>解析サポート機能（コレクション向け）</summary>
    internal static class TomlCollectionHelper
    {
        #region "methods"

        /// <summary>インラインテーブルが閉じられていることを確認する。</summary>
        /// <param name="iter"></param>
        /// <returns>閉じられていたら 1、次のキー取得ならば 2。</returns>
        internal static int CloseInlineTable(this TomlInnerBuffer.TomlIter iter)
        {
            UTF8 c;

            while ((c = iter.GetChar(0)).ch1 != 0) {
                iter.SkipLineFeedAndSpace();

                switch (iter.GetChar(0).ch1) {
                    case (byte)'}':
                        // インラインテーブルが閉じられた
                        iter.Skip(1);
                        return 1;

                    case (byte)',':
                        // 続けて次のキー／値判定へ
                        iter.Skip(1);
                        return 2;

                    default:
                        // インラインテーブルが閉じられていない
                        goto EXIT_WHILE;
                }
            }
            EXIT_WHILE:
            throw new TomlAnalisysException(Resources.INLINE_TABLE_NOT_CLOSE_ERR, iter);
        }

        /// <summary>配列が閉じられていることを確認する。</summary>
        /// <param name="iter"></param>
        /// <returns>閉じられていたら 1、次のキー取得ならば 2。</returns>
        internal static int CloseValueArray(this TomlInnerBuffer.TomlIter iter)
        {
            UTF8 c;

            while ((c = iter.GetChar(0)).ch1 != 0) {
                iter.SkipLineFeedAndSpace();

                switch (iter.GetChar(0).ch1) {
                    case (byte)']':
                        // 配列が閉じられた
                        iter.Skip(1);
                        return 1;

                    case (byte)',':
                        // 続けて次のキー／値判定へ
                        iter.Skip(1);
                        return 2;

                    default:
                        // インラインテーブルが閉じられていない
                        goto EXIT_WHILE;
                }
            }
            EXIT_WHILE:
            throw new TomlAnalisysException(Resources.ARRAY_NOT_CLOSE_ERR, iter);
        }

        /// <summary>パスを走査して、テーブルを取得する。</summary>
        /// <param name="table">カレントテーブル。</param>
        /// <param name="keyStr">キー名称。</param>
        /// <param name="answer">取得したテーブル。</param>
        /// <returns>0はエラー、1は作成済みのテーブル、2は新規作成。</returns>
        internal static int SearchPathTable(this TomlTable table, string keyStr, out TomlTable answer)
        {
            ITomlValue tmp;

            if (table.Contains(keyStr)) {
                // 指定のキーが登録済みならば、紐付く項目を返す
                //
                // 1. テーブルを返す
                // 2. テーブル配列のテーブルを返す
                // 3. テーブルの新規作成を依頼
                switch (table.Member(keyStr).ValueType) {
                    case TomlValueType.TomlTableValue:      // 1
                        answer = (TomlTable)table.Member(keyStr);
                        return 1;

                    case TomlValueType.TomlTableArrayValue: // 2
                        tmp = table.Member(keyStr);
                        answer = (TomlTable)tmp[tmp.Length - 1];
                        return 1;

                    default:
                        answer = null;
                        return 0;
                }
            }
            else {
                answer = null;                              // 3
                return 2;
            }
        }

        /// <summary>']' の後、次が改行／終端／コメントならば真を返す。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>改行／終端／コメントならば真。</returns>
        internal static bool CloseTable(this TomlInnerBuffer.TomlIter iter)
        {
            UTF8 c;

            // 空白を読み飛ばす
            iter.SkipSpace();
       
            // ] の判定
            if (iter.GetChar(0).ch1 != ']') {
                return false;
            }
            iter.Skip(1);

            // 空白を読み飛ばす
            iter.SkipSpace();

            // 文字を判定
            c = iter.GetChar(0);
            return (c.ch1 == '#' || c.ch1 == '\r' || c.ch1 == '\n' || c.ch1 == 0);
        }

        /// <summary>']]' の後、次が改行／終端／コメントならば真を返す。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>改行／終端／コメントならば真。</returns>
        internal static bool CloseTableArray(this TomlInnerBuffer.TomlIter iter)
        {
            UTF8 c;

            // 空白を読み飛ばす
            iter.SkipSpace();

            // ]] の判定
            if (iter.GetChar(0).ch1 != ']' ||
                iter.GetChar(1).ch1 != ']') {
                return false;
            }
            iter.Skip(2);

            // 空白を読み飛ばす
            iter.SkipSpace();

            // 文字を判定
            c = iter.GetChar(0);
            return (c.ch1 == '#' || c.ch1 == '\r' || c.ch1 == '\n' || c.ch1 == 0);
        }

        #endregion
    }
}
