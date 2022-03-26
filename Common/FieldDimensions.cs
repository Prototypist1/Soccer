namespace Common
{
    public struct FieldDimensions
    {

        public double xMax;
        public double yMax;

        public static FieldDimensions Default = new FieldDimensions
        {
            xMax = 6 * 16 * 10_000, //2 * 16 * 2500,
            yMax = 6 * 9 * 10_000,//2 * 9 * 2500
        };
    }
}
