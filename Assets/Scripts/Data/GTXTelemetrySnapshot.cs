namespace GTX.Data
{
    public struct GTXTelemetrySnapshot
    {
        public float speedKph;
        public int gear;
        public float rpm;
        public float rpm01;
        public float boost01;
        public float heat01;
        public float flow01;
        public bool isDrifting;
        public bool isBoosting;
        public string feedback;

        public static GTXTelemetrySnapshot Idle
        {
            get
            {
                return new GTXTelemetrySnapshot
                {
                    gear = 1,
                    rpm = 900f,
                    rpm01 = 0.12f,
                    feedback = "Ready"
                };
            }
        }
    }
}
