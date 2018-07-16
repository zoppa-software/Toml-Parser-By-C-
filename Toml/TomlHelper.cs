using System;
using System.Collections.Generic;
using System.Text;

namespace Toml
{
    /// <summary>解析サポート機能。</summary>
    internal static class TomlHelper
    {
        #region "methods"

        /// <summary>行の開始を判定する。</summary>
        /// <returns>行のデータ種類。</returns>
        internal static TomlInnerBuffer.LineType CheckStartLineToken(this TomlInnerBuffer.TomlIter iter)
        {
            // テーブル配列の開始か判定する
            if (iter.RemnantLength >= 2) {
                if (iter.GetChar(0).ch1 == '[' && iter.GetChar(1).ch1 == '[') {
                    iter.Skip(2);
                    return TomlInnerBuffer.LineType.TomlTableArrayLine;
                }
            }

            // その他の開始を判定する
            //
            // 1. テーブルの開始か判定する
            // 2. コメントの開始か判定する
            // 4. キーの開始か判定する
            // 5. それ以外はエラー
            if (iter.RemnantLength > 0) {
                switch (iter.GetChar(0).ch1) {
                    case (byte)'[':           // 1
                        iter.Skip(1);
                        return TomlInnerBuffer.LineType.TomlTableLine;

                    case (byte)'#':           // 2
                        iter.Skip(1);
                        return TomlInnerBuffer.LineType.TomlCommenntLine;

                    case (byte)'\r':          // 3
                    case (byte)'\n':
                    case (byte)'\0':
                        return TomlInnerBuffer.LineType.TomlLineNone;

                    default:                // 4
                        return TomlInnerBuffer.LineType.TomlKeyValueLine;
                }
            }
            else {                          // 5
                return TomlInnerBuffer.LineType.TomlLineNone;
            }
        }

        //---------------------------------------------------------------------
        // 文字解析
        //---------------------------------------------------------------------
        /// <summary>キー文字列の取得。</summary>
        /// <param name="buffer">内部バッファ。</param>
        /// <param name="iter">イテレータ。</param>
        /// <returns>キーリスト。</returns>
        internal static List<string> GetKeys(this TomlInnerBuffer.TomlIter iter)
        {
            var res = new List<string>();

            while (iter.GetChar(0).ch1 > 0) {
                iter.SkipSpace();               // 1

                var key = iter.GetKey();        // 2
                if (key != null) {
                    res.Add(key);
                    if (iter.GetChar(0).ch1 != '.') {   // 3
                        break;
                    }
                    iter.Skip(1);
                }
                else {
                    break;
                }
            }
            return res;
        }

        /// <summary>キー文字列を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>キー文字列。</returns>
        internal static string GetKey(this TomlInnerBuffer.TomlIter iter)
        {
            var buf = new List<byte>();
            UTF8 c;

            // 空白を読み飛ばす
            iter.SkipSpace();

            // 範囲の開始、終了位置で評価
            //
            // 1. 一文字取得する
            // 2. キー使用可能文字か判定する
            // 3. " なら文字列としてキー文字列を取得する
            while ((c = iter.GetChar(0)).ch1 != 0) {        // 1
                if ((c.ch1 >= 'a' && c.ch1 <= 'z') ||       // 2
                    (c.ch1 >= 'A' && c.ch1 <= 'Z') ||
                    (c.ch1 >= '0' && c.ch1 <= '9') ||
                    c.ch1 == '_' || c.ch1 == '-') {
                    c.Expand(buf);
                    iter.Skip(1);
                }
                else if (c.ch1 == '"' && buf.Count <= 0) {  // 3
                    iter.Skip(1);
                    return iter.GetStringValue();
                }
                else {
                    break;
                }
            }

            // バイトリストを文字列に変換して返す
            if (buf.Count > 0) {
                return Encoding.UTF8.GetString(buf.ToArray());
            }
            else {
                throw new TomlAnalisysException("キー文字列の解析に失敗", iter);
            }
        }

        /// <summary>" で囲まれた文字列を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>取得した文字列。</returns>
        public static string GetStringValue(this TomlInnerBuffer.TomlIter iter)
        {
            var buf = new List<byte>();
            UTF8 c;

            while ((c = iter.GetChar(0)).ch1 != 0) {
                switch (c.ch1) {
                    case (byte)'"':
                        // " を取得したら文字列終了
                        iter.Skip(1);
                        return (buf.Count > 0 ? Encoding.UTF8.GetString(buf.ToArray()) : "");

                    case (byte)'\\':
                        // \のエスケープ文字判定
                        AppendEscapeChar(iter, buf);
                        break;

                    default:
                        // 上記以外は普通の文字として取得
                        c.Expand(buf);
                        iter.Skip(1);
                        break;
                }
            }

            // " で終了できなかったためエラー
            throw new TomlAnalisysException("文字列定義エラー", iter);
        }

        /// <summary>' で囲まれた文字列を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>取得した文字列。</returns>
        public static string GetLiteralStringValue(this TomlInnerBuffer.TomlIter iter)
        {
            var buf = new List<byte>();
            UTF8 c;

            while ((c = iter.GetChar(0)).ch1 != 0) {
                switch (c.ch1) {
                    case (byte)'\'':
                        // " を取得したら文字列終了
                        iter.Skip(1);
                        return (buf.Count > 0 ? Encoding.UTF8.GetString(buf.ToArray()) : "");

                    default:
                        // 上記以外は普通の文字として取得
                        c.Expand(buf);
                        iter.Skip(1);
                        break;
                }
            }

            // ' で終了できなかったためエラー
            throw new TomlAnalisysException("リテラル文字列定義エラー", iter);
        }

        /// <summary>'\'エスケープ文字を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="buf">バイトリスト。</param>
        private static void AppendEscapeChar(TomlInnerBuffer.TomlIter iter, List<byte> buf)
        {
            if (iter.RemnantLength > 2) {
                UTF8 c = iter.GetChar(1);

                switch (c.ch1) {
                    case (byte)'b':
                        buf.Add((byte)'\b');
                        iter.Skip(2);
                        break;
                    case (byte)'t':
                        buf.Add((byte)'\t');
                        iter.Skip(2);
                        break;
                    case (byte)'n':
                        buf.Add((byte)'\n');
                        iter.Skip(2);
                        break;
                    case (byte)'f':
                        buf.Add((byte)'\f');
                        iter.Skip(2);
                        break;
                    case (byte)'r':
                        buf.Add((byte)'\r');
                        iter.Skip(2);
                        break;
                    case (byte)'"':
                        buf.Add((byte)'"');
                        iter.Skip(2);
                        break;
                    case (byte)'/':
                        buf.Add((byte)'/');
                        iter.Skip(2);
                        break;
                    case (byte)'\\':
                        buf.Add((byte)'\\');
                        iter.Skip(2);
                        break;

                    case (byte)'u':
                        if (iter.RemnantLength >= 6) {
                            iter.Skip(2);
                            AppendUnicode(iter, 4, buf);
                        }
                        break;

                    case (byte)'U':
                        if (iter.RemnantLength >= 10) {
                            iter.Skip(2);
                            AppendUnicode(iter, 8, buf);
                        }
                        break;

                    default:
                        throw new TomlAnalisysException("無効なエスケープ文字が指定された", iter);
                }
            }
        }

        /// <summary>UNICODEエスケープ判定。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="len">読み込む文字数。</param>
        /// <param name="buf">バイトリスト。</param>
        private static void AppendUnicode(TomlInnerBuffer.TomlIter iter, int len, List<byte> buf)
        {
            // 16進数文字を判定し、数値化
            uint val = 0;
            for (int i = 0; i < len; ++i) {
                UTF8 c = iter.GetChar(i);
                val <<= 4;

                if (c.ch1 >= '0' && c.ch1 <= '9') {
                    val |= (uint)(c.ch1 - '0');
                }
                else if (c.ch1 >= 'a' && c.ch1 <= 'f') {
                    val |= (uint)(10 + c.ch1 - 'a');
                }
                else if (c.ch1 >= 'A' && c.ch1 <= 'F') {
                    val |= (uint)(10 + c.ch1 - 'A');
                }
                else {
                    throw new TomlAnalisysException("ユニコード文字定義の解析に失敗", iter);
                }
            }

            // UTF8へ変換
            if (val < 0x80) {
                buf.Add((byte)(val & 0xff));
            }
            else if (val < 0x800) {
                buf.Add((byte)(0xc0 | (val >> 6)));
                buf.Add((byte)(0x80 | (val & 0x3f)));
            }
            else if (val < 0x10000) {
                buf.Add((byte)(0xe0 | (val >> 12)));
                buf.Add((byte)(0x80 | ((val >> 6) & 0x3f)));
                buf.Add((byte)(0x80 | (val & 0x3f)));
            }
            else if (val < 0x200000) {
                buf.Add((byte)(0xf0 | (val >> 18)));
                buf.Add((byte)(0x80 | ((val >> 12) & 0x3f)));
                buf.Add((byte)(0x80 | ((val >> 6) & 0x3f)));
                buf.Add((byte)(0x80 | (val & 0x3f)));
            }
            else if (val < 0x4000000) {
                buf.Add((byte)(0xf8 | (val >> 24)));
                buf.Add((byte)(0x80 | ((val >> 18) & 0x3f)));
                buf.Add((byte)(0x80 | ((val >> 12) & 0x3f)));
                buf.Add((byte)(0x80 | ((val >> 6) & 0x3f)));
                buf.Add((byte)(0x80 | (val & 0x3f)));
            }
            else {
                buf.Add((byte)(0xfc | (val >> 30)));
                buf.Add((byte)(0x80 | ((val >> 24) & 0x3f)));
                buf.Add((byte)(0x80 | ((val >> 18) & 0x3f)));
                buf.Add((byte)(0x80 | ((val >> 12) & 0x3f)));
                buf.Add((byte)(0x80 | ((val >> 6) & 0x3f)));
                buf.Add((byte)(0x80 | (val & 0x3f)));
            }
            iter.Skip(len);
        }

        //---------------------------------------------------------------------
        // 定数リテラル解析
        //---------------------------------------------------------------------
        /// <summary>数値（定数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="value">値（戻り値）</param>
        /// <returns>値が取得できたならば真。</returns>
        internal static bool AnalisysKeyword(this TomlInnerBuffer.TomlIter iter, out ITomlValue value)
        {
            // 真偽値（偽）を返す
            if (iter.RemnantLength >= 5) {
                if (iter.GetChar(4).ch1 == 'e' &&
                    iter.GetChar(3).ch1 == 's' &&
                    iter.GetChar(2).ch1 == 'l' &&
                    iter.GetChar(1).ch1 == 'a' &&
                    iter.GetChar(0).ch1 == 'f') {
                    value = TomlValue.False;
                    iter.Skip(5);
                    return true;
                }
            }

            if (iter.RemnantLength >= 4) {
                // 真偽値（真）を返す
                if (iter.GetChar(3).ch1 == 'e' &&
                    iter.GetChar(2).ch1 == 'u' &&
                    iter.GetChar(1).ch1 == 'r' &&
                    iter.GetChar(0).ch1 == 't') {
                    value = TomlValue.True;
                    iter.Skip(4);
                    return true;
                }

                // 正の無限値を返す
                if (iter.GetChar(3).ch1 == 'f' &&
                    iter.GetChar(2).ch1 == 'n' &&
                    iter.GetChar(1).ch1 == 'i' &&
                    iter.GetChar(0).ch1 == '+') {
                    value = TomlValue.Create(double.PositiveInfinity);
                    iter.Skip(4);
                    return true;
                }

                // 負の無限値を返す
                if (iter.GetChar(3).ch1 == 'f' &&
                    iter.GetChar(2).ch1 == 'n' &&
                    iter.GetChar(1).ch1 == 'i' &&
                    iter.GetChar(0).ch1 == '-') {
                    value = TomlValue.Create(double.NegativeInfinity);
                    iter.Skip(4);
                    return true;
                }

                // 正の非数値を返す
                if (iter.GetChar(3).ch1 == 'n' &&
                    iter.GetChar(2).ch1 == 'a' &&
                    iter.GetChar(1).ch1 == 'n' &&
                    iter.GetChar(0).ch1 == '+') {
                    value = TomlValue.Create(double.NaN);
                    iter.Skip(4);
                    return true;
                }

                // 負の非数値を返す
                if (iter.GetChar(3).ch1 == 'n' &&
                    iter.GetChar(2).ch1 == 'a' &&
                    iter.GetChar(1).ch1 == 'n' &&
                    iter.GetChar(0).ch1 == '-') {
                    value = TomlValue.Create(-double.NaN);
                    iter.Skip(4);
                    return true;
                }
            }

            if (iter.RemnantLength >= 3) {
                // 無限値を返す
                if (iter.GetChar(2).ch1 == 'f' &&
                    iter.GetChar(1).ch1 == 'n' &&
                    iter.GetChar(0).ch1 == 'i') {
                    value = TomlValue.Create(double.PositiveInfinity);
                    iter.Skip(3);
                    return true;
                }

                // 非数値を返す
                if (iter.GetChar(2).ch1 == 'n' &&
                    iter.GetChar(1).ch1 == 'a' &&
                    iter.GetChar(0).ch1 == 'n') {
                    value = TomlValue.Create(double.NaN);
                    iter.Skip(3);
                    return true;
                }
            }

            value = null;
            return false;
        }

        //---------------------------------------------------------------------
        // 文字列解析
        //---------------------------------------------------------------------
        /// <summary>""" で囲まれた文字列を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>取得した文字列。</returns>
        internal static string GetMultiStringValue(this TomlInnerBuffer.TomlIter iter)
        {
            var buf = new List<byte>();
            UTF8 c, nc;
            bool eof;
            bool skipSpace = false;
            byte[] lastC = new byte[2];

            // 先頭の改行は取り除く
            iter.SkipHeadLineFeed();

            do {
                eof = iter.IsEndStream;

                while ((c = iter.GetChar(0)).ch1 != 0) {
                    switch (c.ch1) {
                        case (byte)'"':
                            // " を取得したら文字列終了
                            if (iter.GetChar(2).ch1 == '"' &&
                                iter.GetChar(1).ch1 == '"') {
                                iter.Skip(3);
                                return Encoding.UTF8.GetString(buf.ToArray());
                            }
                            else {
                                buf.Add(c.ch1);
                                iter.Skip(1);
                            }
                            break;

                        case (byte)'\\':
                            // \のエスケープ文字判定
                            nc = iter.GetChar(1);
                            if ((nc.ch1 == '\r' || nc.ch1 == '\n' || nc.ch1 == '\t' || nc.ch1 == ' ') &&
                                iter.CheckLineEnd()) {
                                skipSpace = true;
                                iter.Skip(1);
                            }
                            else {
                                AppendEscapeChar(iter, buf);
                            }
                            break;

                        case (byte)' ':
                        case (byte)'\t':
                            // 空白文字を追加する
                            if (!skipSpace) {
                                buf.Add(c.ch1);
                            }
                            iter.Skip(1);
                            break;

                        default:
                            // 上記以外は普通の文字として取得
                            if (c.ch1 > 0x1f) {
                                skipSpace = false;
                                c.Expand(buf);
                            }
                            else {
                                lastC[1] = lastC[0];
                                lastC[0] = (byte)(c.ch1 & 0x1f);
                            }
                            iter.Skip(1);
                            break;
                    }
                }

                // 改行の読み飛ばし指定がなければ追加する
                if (!skipSpace) {
                    if (lastC[1] == '\r' && lastC[0] == '\n') {
                        buf.Add((byte)'\r');
                        buf.Add((byte)'\n');

                    }
                    else if (lastC[0] == '\n') {
                        buf.Add((byte)'\n');
                    }
                }
                iter.ReadLine();

            } while (!eof);

            // " で終了できなかったためエラー
            throw new TomlAnalisysException("複数クォーテーション文字列定義エラー", iter);
        }

        /// <summary>"'" で囲まれた文字列を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>取得した文字列。</returns>
        internal static string GetMultiLiteralStringValue(this TomlInnerBuffer.TomlIter iter)
        {
            var buf = new List<byte>();
            UTF8 c;
            bool eof;

            // 先頭の改行は取り除く
            iter.SkipHeadLineFeed();

            do {
                eof = iter.IsEndStream;

                while ((c = iter.GetChar(0)).ch1 != 0) {
                    switch (c.ch1) {
                        case (byte)'\'':
                            // " を取得したら文字列終了
                            if (iter.GetChar(2).ch1 == '\'' &&
                                iter.GetChar(1).ch1 == '\'') {
                                iter.Skip(3);
                                return Encoding.UTF8.GetString(buf.ToArray());
                            }
                            else {
                                buf.Add(c.ch1);
                                iter.Skip(1);
                            }
                            break;

                        default:
                            // 上記以外は普通の文字として取得
                            c.Expand(buf);
                            iter.Skip(1);
                            break;
                    }
                }
                iter.ReadLine();

            } while (!eof);

            // " で終了できなかったためエラー
            throw new TomlAnalisysException("複数リテラル文字列定義エラー", iter);
        }

        //-----------------------------------------------------------------------------
        // 数値／日付解析
        //-----------------------------------------------------------------------------
        /// <summary>数値（日付／整数／実数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>値。</returns>
        internal static ITomlValue GetNumberOrDateValue(this TomlInnerBuffer.TomlIter iter)
        {
            int year, hour;

            if (ConvertPartitionNumber(iter, 0, 4, out year) &&
                iter.GetChar(4).ch1 == '-') {
                // 日付（年月日）を取得する
                return TomlValue.Create(ConvertDateFormat(iter, year));
            }
            else if (ConvertPartitionNumber(iter, 0, 2, out hour) &&
                     iter.GetChar(2).ch1 == ':') {
                // 日付（時分秒）を取得する
                return TomlValue.Create(ConvertTimeFormat(iter, hour));
            }
            else {
                // 数値（整数／実数）を取得する
                return iter.GetNumberValue(false);
            }
        }

        /// <summary>文字列を部分的に切り取り、数値表現か確認、取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="nest">ネスト位置。</param>
        /// <param name="digit">必要桁数。</param>
        /// <param name="result">読込結果（戻り値）</param>
        /// <returns>取得できたら真。</returns>
        private static bool ConvertPartitionNumber(TomlInnerBuffer.TomlIter iter,
                                                   int nest, int digit, out int result)
        {
            if (iter.RemnantLength < digit) {
                // 指定桁数未満の数値であるためエラー
                result = 0;
                return false;
            }
            else {
                // 必要桁数分ループし、全てが数値表現であることを確認
                // 1. ループ
                // 2. 数値判定し、結果を作成する
                // 3. 結果を返す
                UTF8 c;
                int num = 0;
                for (int i = 0; i < digit; ++i) {       // 1
                    c = iter.GetChar(nest + i);
                    if (c.ch1 >= '0' && c.ch1 <= '9') { // 2
                        num = num * 10 + (c.ch1 - '0');
                    }
                    else {
                        result = 0;
                        return false;
                    }
                }
                result = num;                           // 3
                return true;
            }
        }

        /// <summary>日付表現の日にち部分を解析する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="year">年。</param>
        /// <returns>日付情報。</returns>
        private static TomlDate ConvertDateFormat(TomlInnerBuffer.TomlIter iter, int year)
        {
            int month, day, hour;
            UTF8 c;

            // 月日の判定
            //
            // 1. 取得できたら格納
            // 2. 取得できなかったらエラーを返す
            if (ConvertPartitionNumber(iter, 5, 2, out month) &&    // 1
                iter.GetChar(7).ch1 == '-' &&
                ConvertPartitionNumber(iter, 8, 2, out day)) {
                // 空実装
            }
            else {                                                  // 2
                throw new TomlAnalisysException("日付が解析できない", iter);
            }

            // 'T' の指定がなければ日にちのみ、終了
            iter.Skip(10);
            c = iter.GetChar(0);
            if (c.ch1 != 'T' && c.ch1 != 't' && c.ch1 != ' ' &&
                (c.ch1 == '\t' || c.ch1 == '#' || c.ch1 == '\r' || c.ch1 == '\n')) {
                return new TomlDate((ushort)year, (byte)month, (byte)day, 0, 0, 0, 0, 0, 0);
            }

            // 時間情報を判定して返す
            iter.Skip(1);
            if (ConvertPartitionNumber(iter, 0, 2, out hour) &&
                iter.GetChar(2).ch1 == ':') {
                var tm = ConvertTimeFormat(iter, hour);
                return new TomlDate((ushort)year, (byte)month, (byte)day,
                                    tm.Hour, tm.Minute, tm.Second,
                                    tm.DecSecond, tm.ZoneHour, tm.ZoneMinute);
            }
            else if (c.ch1 == ' ') {
                return new TomlDate((ushort)year, (byte)month, (byte)day, 0, 0, 0, 0, 0, 0);
            }
            else {
                throw new TomlAnalisysException("日付が解析できない", iter);
            }
        }

        /// <summary>日付表現の時間部分を解析する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="hour">時値。</param>
        /// <returns>日付値。</returns>
        private static TomlDate ConvertTimeFormat(TomlInnerBuffer.TomlIter iter, int hour)
        {
            int minute, second, decSec = 0, z_hor, z_min;
            UTF8 c;

            // 分、秒の判定
            //
            // 1. 取得できたら格納
            // 2. 取得できなかったらエラーを返す
            if (ConvertPartitionNumber(iter, 3, 2, out minute) &&   // 1
                iter.GetChar(5).ch1 == ':' &&
                ConvertPartitionNumber(iter, 6, 2, out second)) {
                // 空実装
            }
            else {                                                  // 2
                throw new TomlAnalisysException("時間が解析できない", iter);
            }

            // ミリ秒の解析
            //
            // 1. '.' があればミリ秒解析開始
            // 2. ミリ秒値を計算
            iter.Skip(8);
            if (iter.GetChar(0).ch1 == '.') {           // 1
                iter.Skip(1);
                while (iter.GetChar(0).ch1 >= '0' &&    // 2
                       iter.GetChar(0).ch1 <= '9') {
                    decSec = decSec * 10 + (iter.GetChar(0).ch1 - '0');
                    iter.Skip(1);
                }
            }

            // 時差の解析
            //
            // 1. UTC指定ならば終了
            // 2. 時差指定ならば、時刻を取り込む
            //    2-1. 時刻の書式に問題がなければ終了
            //    2-2. 時刻の書式に問題があればエラー
            // 3. 時差指定なし、正常終了
            c = iter.GetChar(0);
            if (c.ch1 == 'Z' || c.ch1 == 'z') {                 // 1
                iter.Skip(1);
                return new TomlDate(0, 0, 0,
                        (byte)hour, (byte)minute, (byte)second, (uint)decSec, 0, 0);
            }
            else if (c.ch1 == '+' || c.ch1 == '-') {
                if (ConvertPartitionNumber(iter, 1, 2, out z_hor) &&
                    iter.GetChar(3).ch1 == ':' &&
                    ConvertPartitionNumber(iter, 4, 2, out z_min)) {
                    iter.Skip(6);
                    return new TomlDate(0, 0, 0,
                            (byte)hour, (byte)minute, (byte)second, (uint)decSec,
                            (sbyte)(c.ch1 == '+' ? z_hor : -z_hor), (byte)z_min);
                }
                else {
                    throw new TomlAnalisysException("時差が解析できない", iter);
                }
            }
            else {
                return new TomlDate(0, 0, 0,                    // 3
                        (byte)hour, (byte)minute, (byte)second, (uint)decSec, 0, 0);
            }
        }

        /// <summary>数値（整数／実数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="numberSign">符号。</param>
        /// <returns>取得した値。</returns>
        internal static ITomlValue GetNumberValue(this TomlInnerBuffer.TomlIter iter, bool numberSign)
        {
            if (!numberSign &&
                iter.RemnantLength >= 3 &&
                iter.GetChar(0).ch1 == '0') {
                switch (iter.GetChar(1).ch1) {
                    case (byte)'x':
                        iter.Skip(2);
                        return Get16NumberValue(iter);
                    case (byte)'o':
                        iter.Skip(2);
                        return Get8NumberValue(iter);
                    case (byte)'b':
                        iter.Skip(2);
                        return Get2NumberValue(iter);
                    default:
                        break;
                }
            }
            //return get_10number_value(number_sign, buffer,
            //                point, next_point, token_type, error);
            return Get10NumberValue(iter, numberSign);
        }

        /// <summary>数値（16進数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>数値。</returns>
        private static ITomlValue Get16NumberValue(TomlInnerBuffer.TomlIter iter)
        {
            ulong v = 0;
            bool ud = false;
            UTF8 c;
            int point = iter.Pointer;

            // 数値を取得する
            while ((c = iter.GetChar(0)).ch1 != 0) {
                // 1. '_'の連続の判定
                // 2. 数値の計算をする
                //    2-1. 数値の有効範囲を超えるならばエラー
                if (c.ch1 == '_') {
                    if (ud) {                               // 1
                        throw new TomlAnalisysException("数値定義に連続してアンダーバーが使用された", iter);
                    }
                    ud = true;
                }
                else if (c.ch1 >= '0' && c.ch1 <= '9') {
                    if (v <= ulong.MaxValue / 16) {         // 2
                        v = v * 16 + (ulong)(c.ch1 - '0');
                    }
                    else {                                  // 2-1
                        throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
                    }
                    ud = false;
                }
                else if (c.ch1 >= 'A' && c.ch1 <= 'F') {
                    if (v <= ulong.MaxValue / 16) {
                        v = v * 16 + (ulong)(c.ch1 - 'A') + 10;
                    }
                    else {                                  // 2-1
                        throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
                    }
                    ud = false;
                }
                else if (c.ch1 >= 'a' && c.ch1 <= 'f') {
                    if (v <= ulong.MaxValue / 16) {
                        v = v * 16 + (ulong)(c.ch1 - 'a') + 10;
                    }
                    else {                                  // 2-1
                        throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
                    }
                    ud = false;
                }
                else {
                    break;
                }
                iter.Skip(1);
            }

            // 一文字の数値もなければ None値を返す
            if (iter.Pointer == point) {
                return TomlValue.Empty;
            }

            if (v <= ulong.MaxValue) {
                return TomlValue.Create(v);
            }
            else {
                throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
            }
        }

        /// <summary>数値（8進数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>数値。</returns>
        private static ITomlValue Get8NumberValue(TomlInnerBuffer.TomlIter iter)
        {
            ulong v = 0;
            bool ud = false;
            UTF8 c;
            int point = iter.Pointer;

            // 数値を取得する
            while ((c = iter.GetChar(0)).ch1 != 0) {
                // 1. '_'の連続の判定
                // 2. 数値の計算をする
                //    2-1. 数値の有効範囲を超えるならばエラー
                if (c.ch1 == '_') {
                    if (ud) {                               // 1
                        throw new TomlAnalisysException("数値定義に連続してアンダーバーが使用された", iter);
                    }
                    ud = true;
                }
                else if (c.ch1 >= '0' && c.ch1 <= '7') {
                    if (v <= ulong.MaxValue / 8) {          // 2
                        v = v * 8 + (ulong)(c.ch1 - '0');
                    }
                    else {                                  // 2-1
                        throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
                    }
                    ud = false;
                }
                else {
                    break;
                }
                iter.Skip(1);
            }

            // 一文字の数値もなければ None値を返す
            if (iter.Pointer == point) {
                return TomlValue.Empty;
            }

            if (v <= ulong.MaxValue) {
                return TomlValue.Create(v);
            }
            else {
                throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
            }
        }

        /// <summary>数値（2進数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <returns>数値。</returns>
        private static ITomlValue Get2NumberValue(TomlInnerBuffer.TomlIter iter)
        {
            ulong v = 0;
            bool ud = false;
            UTF8 c;
            int point = iter.Pointer;

            // 数値を取得する
            while ((c = iter.GetChar(0)).ch1 != 0) {
                // 1. '_'の連続の判定
                // 2. 数値の計算をする
                //    2-1. 数値の有効範囲を超えるならばエラー
                if (c.ch1 == '_') {
                    if (ud) {                               // 1
                        throw new TomlAnalisysException("数値定義に連続してアンダーバーが使用された", iter);
                    }
                    ud = true;
                }
                else if (c.ch1 == '0' || c.ch1 == '1') {
                    if (v <= ulong.MaxValue / 2) {          // 2
                        v = v * 2 + (ulong)(c.ch1 - '0');
                    }
                    else {                                  // 2-1
                        throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
                    }
                    ud = false;
                }
                else {
                    break;
                }
                iter.Skip(1);
            }

            // 一文字の数値もなければ None値を返す
            if (iter.Pointer == point) {
                return TomlValue.Empty;
            }

            if (v <= ulong.MaxValue) {
                return TomlValue.Create(v);
            }
            else {
                throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
            }
        }

        /// <summary>数値（整数／実数）を取得する。</summary>
        /// <param name="iter">イテレータ。</param>
        /// <param name="numberSign">符号。</param>
        /// <returns>数値。</returns>
        private static ITomlValue Get10NumberValue(TomlInnerBuffer.TomlIter iter,
                                                   bool numberSign)
        {
            ulong v = 0;
            bool ud = false;
            UTF8 c;
            int point = iter.Pointer;
            int digit = -1;
            int expo = -1;
            int exp_v = -1;
            bool ld_zero = false, lst_zero = false;

            // 仮数部を取得する
            while ((c = iter.GetChar(0)).ch1 != 0) {
                // 1. '_'の連続の判定
                // 2. 数値の計算をする
                //    2-1. 数値の有効範囲を超えるならばエラー
                // 3. 小数点位置を取得する
                // 4. 指数部（e）を取得する
                if (c.ch1 == '_') {
                    if (ud) {                           // 1
                        throw new TomlAnalisysException("数値定義に連続してアンダーバーが使用された", iter);
                    }
                    ud = true;
                }
                else if (c.ch1 >= '0' && c.ch1 <= '9') {
                    if (v < ulong.MaxValue / 10) {      // 2
                        v = v * 10 + (ulong)(c.ch1 - '0');
                        if (digit >= 0) {
                            digit++;
                            lst_zero = true;
                        }
                        else {
                            ld_zero = true;
                        }
                    }
                    else {                              // 2-1
                        throw new TomlAnalisysException("整数値が有効範囲を超えている", iter);
                    }
                    ud = false;
                }
                else if (c.ch1 == '.') {                // 3
                    if (ld_zero && digit < 0) {
                        digit = 0;
                    }
                    else if (ld_zero) {
                        throw new TomlAnalisysException("複数の小数点が定義された", iter);
                    }
                    else {
                        throw new TomlAnalisysException("小数点の前に数値が入力されていない", iter);
                    }
                }
                else if (c.ch1 == 'e' || c.ch1 == 'E') {
                    expo = 0;                           // 4
                    break;
                }
                else {
                    break;
                }
                iter.Skip(1);
            }

            // 一文字の数値もなければ None値を返す
            if (iter.Pointer == point) {
                return TomlValue.Empty;
            }

            // 仮数部を取得する
            CalcExponentConvert(iter, digit, expo, out exp_v);

            if (digit < 0 && expo < 0) {
                // 整数値を取得する
                //
                // 1. 負の整数変換
                // 2. 正の整数変換
                if (numberSign) {
                    if (v <= (ulong)long.MaxValue) {        // 1
                        return TomlValue.Create(-(long)v);
                    }
                    else if (v == (ulong)long.MaxValue + 1) {
                        return TomlValue.Create(long.MinValue);
                    }
                    else { 
                        throw new TomlAnalisysException("実数値が有効範囲を超えている", iter);
                    }
                }
                else {
                    if (v <= long.MaxValue) {               //2
                        return TomlValue.Create((long)v);
                    }
                    else {
                        throw new TomlAnalisysException("実数値が有効範囲を超えている", iter);
                    }
                }
            }
            else {
                if (digit >= 0 && !lst_zero) {
                    throw new TomlAnalisysException("小数点の後に数値が入力されていない", iter);
                }

                // 実数値を取得する
                //
                // 1. 負の指数なら除算
                // 2. 正の指数なら積算
                // 3. 0の指数なら使用しない
                // 4. 値を保持
                double dv = 0;
                if (exp_v < 0) {
                    double abs_e = 1;                   // 1
                    for (int i = 0; i < Math.Abs(exp_v); ++i) {
                        abs_e *= 10;
                    }
                    dv = (double)v / abs_e;
                }
                else if (exp_v > 0) {
                    double abs_e = 1;                   // 2
                    for (int i = 0; i < Math.Abs(exp_v); ++i) {
                        abs_e *= 10;
                    }
                    dv = (double)v* abs_e;
                }
                else {
                    dv = (double)v;                     // 3
                }
                if (dv <= double.MaxValue) {            // 4
                    return TomlValue.Create(numberSign ? (double)-dv : (double)dv);
                }
                else {
                    throw new TomlAnalisysException("実数値が有効範囲を超えている", iter);
                }
            }
        }

        /// <summary>実数の指数部を取得する。</summary>
        /// <param name="iter"></param>
        /// <param name="digit">小数点位置。</param>
        /// <param name="expo">指数(e)。</param>
        /// <param name="resExpo">指数値（戻り値）</param>
        private static void CalcExponentConvert(TomlInnerBuffer.TomlIter iter,
                                                int digit,
                                                int expo,
                                                out int resExpo)
        {
            bool ud = false;
            UTF8 c;
            bool sign = false;
            int exp_v = 0;
            int point = iter.Pointer;

            if (expo >= 0) {
                // 符号を取得する
                iter.Skip(1);
                c = iter.GetChar(0);

                if (c.ch1 == '+') {
                    sign = false;
                    iter.Skip(1);
                }
                else if (c.ch1 == '-') {
                    sign = true;
                    iter.Skip(1);
                }

                ud = false;
                while ((c = iter.GetChar(0)).ch1 != 0) {
                    // 1. '_'の連続の判定
                    // 2. 指数部を計算する
                    if (c.ch1 == '_') {
                        if (ud) {                               // 1
                            throw new TomlAnalisysException("数値定義に連続してアンダーバーが使用された", iter);
                        }
                        ud = true;
                    }
                    else if (c.ch1 >= '0' && c.ch1 <= '9') {
                        exp_v = exp_v * 10 + (c.ch1 - '0');     // 2
                        if (exp_v >= 308) {
                            throw new TomlAnalisysException("実数の表現が範囲外", iter);
                        }
                        ud = false;
                    }
                    else {
                        break;
                    }
                    iter.Skip(1);
                }

                // 指数値を確認する
                // 1. 指数値が 0以下でないことを確認
                // 2. 指数値が '0'始まりでないことを確認
                if (exp_v <= 0) {                               // 1
                    throw new TomlAnalisysException("実数の表現が範囲外", iter);
                }
                else if (iter.GetChar(0).ch1 == '0') {          // 2
                    throw new TomlAnalisysException("数値の先頭に無効な 0がある", iter);
                }
            }

            // 符号を設定
            if (sign) { exp_v = -exp_v; }

            // 小数点位置とマージ
            exp_v -= (digit > 0 ? digit : 0);
            if (exp_v > 308 || exp_v < -308) {
                throw new TomlAnalisysException("実数の表現が範囲外", iter);
            }

            resExpo = exp_v;
        }

        #endregion
    }
}
