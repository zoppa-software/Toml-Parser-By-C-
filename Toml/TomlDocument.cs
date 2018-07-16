using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Toml
{
    /// <summary>Tomlドキュメント。</summary>
    public sealed class TomlDocument
        : ITomlValue
    {
        #region "fields"

        /// <summary>ルートテーブル。</summary>
        private readonly TomlTable root;

        /// <summary>カレントテーブル参照。</summary>
        private TomlTable current;

        #endregion

        #region "properties"

        /// <summary>要素の種類を取得する。</summary>
        public TomlValueType ValueType => TomlValueType.TomlTableValue;

        /// <summary>値を取得する。</summary>
        public object Raw => this.root.Raw;

        /// <summary>値をインデックスで取得する。</summary>
        /// <param name="index">インデックス。</param>
        /// <returns>値。</returns>
        public object this[int index] => this.root[index];

        /// <summary>値の数を取得する。</summary>
        public int Length => this.root.Length;

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        public TomlDocument()
        {
            this.root = new TomlTable();
            this.current = this.root;
        }

        #endregion

        #region "methods"

        //---------------------------------------------------------------------
        // テーブル操作
        //---------------------------------------------------------------------
        /// <summary>指定のキーの値を取得する。</summary>
        /// <param name="key">キー。</param>
        /// <returns>値。</returns>
        public ITomlValue Member(string key)
        {
            return this.root.Member(key);
        }

        /// <summary>キーと値を登録する。</summary>
        /// <param name="key">キー。</param>
        /// <param name="value">値。</param>
        public void AddKeyAndValue(string key, ITomlValue value)
        {
            this.root.AddKeyAndValue(key, value);
        }

        /// <summary>列挙子を取得する。</summary>
        /// <returns>列挙子。</returns>
        public IEnumerator<KeyValuePair<string, ITomlValue>> GetEnumerator()
        {
            return this.root.GetEnumerator();
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
            return this.root.ToString();
        }

        //---------------------------------------------------------------------
        // 読み込み
        //---------------------------------------------------------------------
        /// <summary>指定文字列からTomlドキュメントを作成する。</summary>
        /// <param name="document">文字列。</param>
        public void Load(string document)
        {
            var bytechar = System.Text.Encoding.UTF8.GetBytes(document);
            using (var sr = new MemoryStream(bytechar)) {
                this.Load(sr);
            }
        }

        /// <summary>指定パスよりTomlドキュメントを作成する。</summary>
        /// <param name="path">ファイルパス。</param>
        /// <returns>読み込めれたら真を返す。</returns>
        public async Task<bool> LoadPathAync(string path)
        {
            return await Task.Run(() => {
                using (var sr = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    this.Load(sr);
                }
                return true;
            });
        }

        /// <summary>指定パスよりTomlドキュメントを作成する。</summary>
        /// <param name="path">ファイルパス。</param>
        /// <returns>読み込めれたら真を返す。</returns>
        public void LoadPath(string path)
        {
            using (var sr = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                this.Load(sr);
            }
        }

        /// <summary>指定ストリームよりTomlドキュメントを作成する。</summary>
        /// <param name="sr">ストリーム。</param>
        public void Load(Stream sr)
        {
            this.root.Clear();
            this.current = this.root;
            var buffer = new TomlInnerBuffer();

            while (this.AppendLine(sr, buffer)) {
                buffer.ClearUTF8();
            };
        }

        //---------------------------------------------------------------------
        // 解析
        //---------------------------------------------------------------------
        /// <summary>一行分のデータを取り込む。</summary>
        /// <param name="sr">読み込みストリーム。</param>
        /// <param name="buffer">内部バッファ。</param>
        /// <returns>残件データがなければ真。</returns>
        private bool AppendLine(Stream sr, TomlInnerBuffer buffer)
        {
            // 一行のデータを取得する
            var readed = buffer.ReadLine(sr);

            // 行のデータ開始を判定する
            // 　行のデータ種類によって、テーブル／キー／コメントの判定を行う
            // 1. コメント
            // 2. キー文字列
            // 3. テーブル
            // 4. テーブル配列
            var iter = buffer.GetIterator(sr);
            iter.SkipSpace();
            switch (iter.CheckStartLineToken()) {
                case TomlInnerBuffer.LineType.TomlCommenntLine:     // 1
                    break;

                case TomlInnerBuffer.LineType.TomlKeyValueLine:     // 2
                    AnalisysKeyAndValue(iter, this.current, false);
                    break;

                case TomlInnerBuffer.LineType.TomlTableLine:        // 3
                    AnalisysTable(iter, this.root);
                    break;

                case TomlInnerBuffer.LineType.TomlTableArrayLine:   // 4
                    AnalisysTableArray(iter, this.root);
                    break;
            }

            return readed;
        }

        //---------------------------------------------------------------------
        // キー／値解析
        //---------------------------------------------------------------------
        /// <summary>キーと値のペアを取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="table">対象テーブル。</param>
        private void AnalisysKeyAndValue(TomlInnerBuffer.TomlIter iter,
                                         TomlTable table,
                                         bool lastNoCheck)
        {
            // キー文字列を取得する
            //
            // 1. キー文字列を取得
            // 2. キー以降の空白を読み飛ばす
            // 3. = で連結しているか確認
            var keyPtr = iter.GetKeys();            // 1
            iter.SkipSpace();                       // 2
            if (iter.GetChar(0).ch1 != '=') {       // 3
                throw new TomlAnalisysException("キー文字列の解析に失敗", iter);
            }

            // 値を取得する
            //
            // 1. 値が取得できるか
            // 2. 無効値であるか
            iter.Skip(1);
            var val = this.AnalisysValue(iter);     // 1
            if (val.ValueType == TomlValueType.TomlNoneValue) {
                return;                             // 2
            }

            // 改行まで確認
            if (!lastNoCheck && !iter.CheckLineEndOrComment()) {
                throw new TomlAnalisysException("値の解析に失敗", iter);
            }

            // '.' で指定されたテーブル参照を収集する
            //
            // 1. テーブル参照を取得
            // 2. エラーが有れば終了
            // 3. 既に作成済みならばカレントを変更
            // 4. 作成されていなければテーブルを作成し、カレントに設定
            TomlTable curTable = table;
            TomlTable newTable = null;
            for (int i = 0; i < keyPtr.Count - 1; ++i) {
                var keystr = keyPtr[i];

                switch (curTable.SearchPathTable(keystr, out newTable)) {   // 1
                    case 0:                                                 // 2
                        throw new TomlAnalisysException("テーブル名を再定義しています", iter);

                    case 1:
                        curTable = newTable;                                // 3
                        break;

                    default:
                        newTable = new TomlTable();                         // 4
                        curTable.AddKeyAndValue(keystr, newTable);
                        curTable = newTable;
                        break;
                }
            }

            // 最終のテーブルに値を割り当てる
            var laststr = keyPtr[keyPtr.Count - 1];
            if (!curTable.Contains(laststr)) {
                curTable.AddKeyAndValue(laststr, val);
            }
            else {
                throw new TomlAnalisysException("キーが再定義された", iter);
            }
        }

        /// <summary>値の解析を行う。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>値情報。</returns>
        private ITomlValue AnalisysValue(TomlInnerBuffer.TomlIter iter)
        {
            ITomlValue value = null;

            // インラインテーブル、配列、日付の確認
            //
            // 1. インラインテーブルを解析する
            // 2. 配列を解析する
            // 3. 値を取得する
            iter.SkipSpace();
            if (iter.GetChar(0).ch1 == '{') {       // 1
                iter.Skip(1);
                value = this.GetInlineTable(iter);
            }
            else if (iter.GetChar(0).ch1 == '[') {  // 2
                iter.Skip(1);
                value = this.GetValueArray(iter);
            }
            else {
                value = this.ConvertValue(iter);    // 3
            }

            return value;
        }

        /// <summary>数値（整数／実数／日付）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>値情報。</returns>
        private ITomlValue ConvertValue(TomlInnerBuffer.TomlIter iter)
        {
            ITomlValue value = null;

            // 値リテラルを解析する
            if (iter.AnalisysKeyword(out value)) {
                return value;
            }

            // 複数ライン文字列を返す
            if (iter.RemnantLength >= 3) {
                // 複数ライン文字列を返す
                if (iter.GetChar(2).ch1 == '"' &&
                    iter.GetChar(1).ch1 == '"' &&
                    iter.GetChar(0).ch1 == '"') {
                    iter.Skip(3);
                    return TomlValue.Create(iter.GetMultiStringValue());
                }

                // 複数ライン文字列（リテラル）を返す
                if (iter.GetChar(2).ch1 == '\'' &&
                    iter.GetChar(1).ch1 == '\'' &&
                    iter.GetChar(0).ch1 == '\'') {
                    iter.Skip(3);
                    return TomlValue.Create(iter.GetMultiLiteralStringValue());
                }
            }

            // 数値／日付／文字列
            if (iter.RemnantLength > 0) {
                switch (iter.GetChar(0).ch1) {
                    case (byte)'"':
                        // 文字列を取得する
                        iter.Skip(1);
                        return TomlValue.Create(iter.GetStringValue());

                    case (byte)'\'':
                        // リテラル文字列を取得する
                        iter.Skip(1);
                        return TomlValue.Create(iter.GetLiteralStringValue());

                    case (byte)'#':
                        // コメントを取得する
                        iter.Skip(1);
                        return null;

                    case (byte)'+':
                        // 数値を取得する
                        iter.Skip(1);
                        return iter.GetNumberValue(false);

                    case (byte)'-':
                        // 数値を取得する
                        iter.Skip(1);
                        return iter.GetNumberValue(true);

                    default:
                        // 日付／数値を取得する
                        return iter.GetNumberOrDateValue();
                }
            }
            else {
                throw new TomlAnalisysException("値が定義されていない", iter);
            }
        }

        /// <summary>インラインテーブルを解析する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>値情報。</returns>
        private ITomlValue GetInlineTable(TomlInnerBuffer.TomlIter iter)
        {
            var table = new TomlTable();
            UTF8 c;

            while ((c = iter.GetChar(0)).ch1 != 0) {
                // 改行、空白部を取り除く
                iter.SkipLineFeedAndSpace();

                // キー／値部分を取り込む
                this.AnalisysKeyAndValue(iter, table, true);

                // インラインテーブルが閉じられているか確認
                //
                // 1. テーブルが閉じられている
                // 2. 次のキー／値を取得
                // 3. エラー
                switch (iter.CloseInlineTable()) {
                    case 1:                         // 1
                        return table;

                    case 2:                         // 2
                        // 空実装
                        break;

                    default:                        // 3
                        break;
                }
            }

            throw new TomlAnalisysException("インラインテーブルが正しく閉じられていない", iter);
        }

        /// <summary>配列を解析する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>値情報。</returns>
        private ITomlValue GetValueArray(TomlInnerBuffer.TomlIter iter)
        {
            var array = new List<ITomlValue>();

            while (iter.GetChar(0).ch1 != 0) {
                // 改行、空白部を取り除く
                iter.SkipLineFeedAndSpace();

                // 値を取り込む
                var value = this.AnalisysValue(iter);
     
                // 空値以外は取り込む
                if (value.ValueType != TomlValueType.TomlNoneValue) {
                    array.Add(value);
                }

                // 配列が閉じられているか確認
                //
                // 1. テーブルが閉じられている
                // 2. 次のキー／値を取得
                // 3. エラー
                switch (iter.CloseValueArray()) {
                    case 1:                         // 1
                        return CheckArrayValueType(array, iter);

                    case 2:                         // 2
                        if (value.ValueType != TomlValueType.TomlNoneValue) {
                            break;
                        }
                        else {
                            throw new TomlAnalisysException("カンマの前に値が定義されていない", iter);
                        }

                    default:                        // 3
                        break;
                }
            }

            // 配列が閉じられていない
            throw new TomlAnalisysException("配列が正しく閉じられていない", iter);
        }

        /// <summary>配列内の値の型が全て一致するか判定する。</summary>
        /// <param name="array">判定する配列参照。</param>
        /// <param name="iter">イテレータ。</param>
        /// <returns>配列情報。</returns>
        private ITomlValue CheckArrayValueType(List<ITomlValue> array,
                                               TomlInnerBuffer.TomlIter iter)
        {
            TomlValueType type = TomlValueType.TomlNoneValue;

            // 全ての要素の型が一致することを確認
            if (array.Count > 0) {
                type = array[0].ValueType;

                for (int i = 1; i < array.Count; ++i) {
                    if (type != array[i].ValueType) {
                        throw new TomlAnalisysException("配列内の値の型が異なる", iter);
                    }
                }
            }
            
            // 配列を作成して返す
            switch (type) {
                case TomlValueType.TomlBooleanValue:
                    var bools = array.Select(x => (bool)x.Raw).ToArray();
                    return TomlValue.Create(bools);

                case TomlValueType.TomlDateValue:
                    var dates = array.Select(x => (TomlDate)x.Raw).ToArray();
                    return TomlValue.Create(dates);

                case TomlValueType.TomlFloatValue:
                    var floats = array.Select(x => (double)x.Raw).ToArray();
                    return TomlValue.Create(floats);

                case TomlValueType.TomlIntegerValue:
                    var longs = array.Select(x => (long)x.Raw).ToArray();
                    return TomlValue.Create(longs);

                case TomlValueType.TomlStringValue:
                    var strings = array.Select(x => (string)x.Raw).ToArray();
                    return TomlValue.Create(strings);

                default:
                    return TomlValue.Create(array.ToArray());
            }
        }

        //---------------------------------------------------------------------
        // テーブル解析
        //---------------------------------------------------------------------
        /// <summary>テーブルを作成する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="table">カレントテーブル。</param>
        private void AnalisysTable(TomlInnerBuffer.TomlIter iter, TomlTable table)
        {
            // '.' で区切られたテーブル名を事前に収集する
            var keyPtr = iter.GetKeys();

            // テーブルが閉じられているか確認
            if (!iter.CloseTable()) {
                throw new TomlAnalisysException("テーブル構文の解析に失敗", iter);
            }

            // テーブルを作成する
            //
            // 1. テーブル参照を取得
            // 2. エラーが有れば終了
            // 3. 既に作成済みならばカレントを変更
            // 4. 作成されていなければテーブルを作成し、カレントに設定
            TomlTable curTable = table;
            TomlTable newTable = null;
            foreach (var keystr in keyPtr) {
                switch (curTable.SearchPathTable(keystr, out newTable)) {    // 1
                    case 0:                                                  // 2
                        throw new TomlAnalisysException("テーブル名を再定義しています", iter);

                    case 1:
                        curTable = newTable;                                // 3
                        break;

                    default:
                        newTable = new TomlTable();                         // 4
                        curTable.AddKeyAndValue(keystr, newTable);
                        curTable = newTable;
                        break;
                }
            }

            // 空白は読み捨てておく
            iter.SkipSpace();

            // カレントのテーブルを設定
            this.current = curTable;
            if (!this.current.IsDefined) {
                this.current.IsDefined = true;
            }
            else {
                throw new TomlAnalisysException("キーが再定義された", iter);
            }
        }

        /// <summary>テーブル配列を作成する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="table">カレントテーブル。</param>
        private void AnalisysTableArray(TomlInnerBuffer.TomlIter iter, TomlTable table)
        {
            // '.' で区切られたテーブル名を事前に収集する
            var keyPtr = iter.GetKeys();

            // テーブル配列が閉じられているか確認
            if (!iter.CloseTableArray()) {
                throw new TomlAnalisysException("テーブル配列構文の解析に失敗", iter);
            }

            // 最下層のテーブル以外のテーブル参照を収集する
            //
            // 1. テーブル参照を取得
            // 2. エラーが有れば終了
            // 3. 既に作成済みならばカレントを変更
            // 4. 作成されていなければテーブルを作成し、カレントに設定
            TomlTable curTable = table;
            TomlTable newTable = null;
            string keystr = "";
            for (int i = 0; i < keyPtr.Count - 1; ++i) {
                keystr = keyPtr[i];

                switch (curTable.SearchPathTable(keystr, out newTable)) {   // 1
                    case 0:                                                 // 2
                        throw new TomlAnalisysException("テーブル名を再定義しています", iter);

                    case 1:
                        curTable = newTable;                                // 3
                        break;

                    default:
                        newTable = new TomlTable();                         // 4
                        curTable.AddKeyAndValue(keystr, newTable);
                        curTable = newTable;
                        break;
                }
            }

            // 最下層のテーブルは新規作成となる
            //
            // 1. 登録する名前（キー文字列）を取得
            // 2. 親のテーブルに最下層のテーブル名が登録されている
            //    2-1. 登録されている名前のデータを取得する
            //    2-2. テーブル配列が登録されているならば、新規テーブルを作成し、カレントテーブルとする
            //    2-3. テーブル配列でないならばエラーとする
            // 3. 親のテーブルに最下層のテーブル名が登録されていない
            //    3-1. テーブル配列を作成し、テーブルを追加、追加されたテーブルが次のカレントテーブルになる
            keystr = keyPtr[keyPtr.Count - 1];                              // 1

            if (curTable.Contains(keystr)) {                                // 2-1
                var val = curTable.Member(keystr);
                if (val.ValueType == TomlValueType.TomlTableArrayValue) {   // 2-2
                    newTable = new TomlTable();
                    ((Array<TomlTable>)val).Add(newTable);
                }
                else {                                                      // 2-3
                    throw new TomlAnalisysException("キーが再定義された", iter);
                }
            }
            else {
                newTable = new TomlTable();                                 // 3-1
                var newArr = TomlValue.Create(new TomlTable[] { newTable });
                curTable.AddKeyAndValue(keystr, newArr);
            }

            // カレントのテーブルを作成したテーブルに変更
            this.current = newTable;
            iter.SkipSpace();
        }

        #endregion
    }
}
