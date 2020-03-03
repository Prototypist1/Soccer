namespace Common
{
    public struct FieldDimensions {

        public double xMax;
        public double yMax;

        public static FieldDimensions Default = new FieldDimensions{
                xMax = 16 * 2500,
                yMax = 9 * 2500
        };
    }
}
