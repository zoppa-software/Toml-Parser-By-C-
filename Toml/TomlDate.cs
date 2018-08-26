namespace Toml
{
    /// <summary>日付データ。</summary>
    public struct TomlDate
    {
        #region "properties"

        /// <summary>値の種類を取得する。</summary>
        public TomlValueType ValueType => TomlValueType.TomlDateValue;

        /// <summary>日付データなら真を返す。</summary>
        public bool IsDate => (this.Year > 0);

        /// <summary>時間データならば真を返す。</summary>
        public bool IsTime => (this.Year == 0);

        /// <summary>UTC ならば真を返す。</summary>
        public bool IsUTC => (this.ZoneHour == 0 && this.ZoneMinute == 0);

        /// <summary>年を取得する。</summary>
        public ushort Year
        {
            get;
            private set;
        }

        /// <summary>月を取得する。</summary>
        public byte Month
        {
            get;
            private set;
        }

        /// <summary>日を取得する。</summary>
        public byte Day
        {
            get;
            private set;
        }

        /// <summary>時を取得する。</summary>
        public byte Hour
        {
            get;
            private set;
        }

        /// <summary>分を取得する。</summary>
        public byte Minute
        {
            get;
            private set;
        }

        /// <summary>秒を取得する。</summary>
        public byte Second
        {
            get;
            private set;
        }

        /// <summary>秒の小数部を取得する。</summary>
        public uint DecSecond
        {
            get;
            private set;
        }

        /// <summary>時差（時間）を取得する。</summary>
        public sbyte ZoneHour
        {
            get;
            private set;
        }

        /// <summary>時差（分）を取得する。</summary>
        public byte ZoneMinute
        {
            get;
            private set;
        }

        #endregion

        #region "constructor"

        /// <summary>コンストラクタ。</summary>
        /// <param name="year">年。</param>
        /// <param name="month">月。</param>
        /// <param name="day">日。</param>
        /// <param name="hour">時。</param>
        /// <param name="minute">分。</param>
        /// <param name="second">秒。</param>
        /// <param name="zoneHour">時差（時間）</param>
        /// <param name="zoneMinute">時差（分）</param>
        public TomlDate(ushort year, byte month, byte day,
                        byte hour, byte minute, byte second, uint decSecond,
                        sbyte zoneHour, byte zoneMinute)
        {
            this.Year = year;
            this.Month = month;
            this.Day = day;
            this.Hour = hour;
            this.Minute = minute;
            this.Second = second;
            this.DecSecond = decSecond;
            this.ZoneHour = zoneHour;
            this.ZoneMinute = zoneMinute;
        }

        #endregion

        #region "methods"

        /// <summary>等価判定。</summary>
        /// <param name="obj">比較対象。</param>
        /// <returns>判定結果。</returns>
        public override bool Equals(object obj)
        {
            if (obj is TomlDate) {
                var other = (TomlDate)obj;
                return (this.Year == other.Year &&
                        this.Month == other.Month &&
                        this.Day == other.Day &&
                        this.Hour == other.Hour &&
                        this.Minute == other.Minute &&
                        this.Second == other.Second &&
                        this.DecSecond == other.DecSecond &&
                        this.ZoneHour == other.ZoneHour &&
                        this.ZoneMinute == other.ZoneMinute);
            }
            else {
                return false;
            }
        }

        /// <summary>ハッシュコード値を取得する。</summary>
        /// <returns>ハッシュコード値。</returns>
        public override int GetHashCode()
        {
            return this.Year.GetHashCode() ^
                   this.Month.GetHashCode() ^
                   this.Day.GetHashCode() ^
                   this.Hour.GetHashCode() ^
                   this.Minute.GetHashCode() ^
                   this.Second.GetHashCode() ^
                   this.DecSecond.GetHashCode() ^
                   this.ZoneHour.GetHashCode() ^
                   this.ZoneMinute.GetHashCode();
        }

        /// <summary>インスタンスの文字列表現を取得する。</summary>
        /// <returns>文字列。</returns>
        public override string ToString()
        {
            if (this.IsDate) {
                if (this.IsUTC) {
                    return string.Format("{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}Z",
                                         this.Year, this.Month, this.Day,
                                         this.Hour, this.Minute,
                                         this.DecSecond > 0 ?
                                                string.Format("{0:00}.{1}", this.Second, this.DecSecond) :
                                                string.Format("{0:00}", this.Second));
                }
                else {
                    return string.Format("{0:0000}-{1:00}-{2:00}T{3:00}:{4:00}:{5:00}{6}:{7:00}",
                                         this.Year, this.Month, this.Day,
                                         this.Hour, this.Minute,
                                         this.DecSecond > 0 ?
                                                string.Format("{0:00}.{1}", this.Second, this.DecSecond) :
                                                string.Format("{0:00}", this.Second),
                                         this.ZoneHour > 0 ?
                                                string.Format("+{0:00}", this.ZoneHour) :
                                                string.Format("{0:00}", this.ZoneHour),
                                         this.ZoneMinute);
                }
            }
            else {
                if (this.IsUTC) {
                    return string.Format("{0:00}:{1:00}:{2:00}",
                                         this.Hour, this.Minute,
                                         this.DecSecond > 0 ?
                                                string.Format("{0:00}.{1}", this.Second, this.DecSecond) :
                                                string.Format("{0:00}", this.Second));
                }
                else {
                    return string.Format("{0:00}:{1:00}:{2:00}Z{3}:{4:00}",
                                         this.Hour, this.Minute,
                                         this.DecSecond > 0 ?
                                                string.Format("{0:00}.{1}", this.Second, this.DecSecond) :
                                                string.Format("{0:00}", this.Second),
                                         this.ZoneHour > 0 ?
                                                string.Format("+{0:00}", this.ZoneHour) :
                                                string.Format("{0:00}", this.ZoneHour),
                                         this.ZoneMinute);
                }
            }
        }

        #endregion
    }
}
