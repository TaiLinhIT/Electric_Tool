namespace Electric_Meter.Configs
{
    public class AppSetting
    {
        public string ConnectString { get; set; }
        public int TimeReloadData { get; set; }
        public int TimeSaveToDataBase { get; set; }
        public int TimeSendRequest { get; set; }
        public Dictionary<string, string> Requests { get; set; } // Danh sách các yêu cầu
        public string CurrentArea { get; set; }
        public string Port { get; set; }
        public int Baudrate { get; set; }
        public int TotalMachine { get; set; }
        public List<string> AutoActions { get; set; }
        public float ResistanceCoefficient { get; set; }
    }
}
