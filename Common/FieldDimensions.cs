namespace Common
{
    public struct FieldDimensions {

        public double xMax;
        public double yMax;

        public static FieldDimensions Default = new FieldDimensions {
            xMax = 4* 16 * 5_000 , //2 * 16 * 2500,
                yMax = 4* 9 * 5_000,//2 * 9 * 2500
        };
    }
}
