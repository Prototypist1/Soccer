namespace Common
{
    public struct FieldDimensions
    {

        public double xMax;
        public double yMax;

        public static FieldDimensions Default = new FieldDimensions
        {
            xMax = 6 * 16 * 5_000, //2 * 16 * 2500,
            yMax = 6 * 9 * 5_000,//2 * 9 * 2500
        };
    }
}
