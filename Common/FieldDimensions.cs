namespace Common
{
    public struct FieldDimensions {

        public double xMax;
        public double yMax;

        public static FieldDimensions Default = new FieldDimensions {
            xMax = 2* 16 * 7500 , //2 * 16 * 2500,
                yMax = 2* 9 * 7500,//2 * 9 * 2500
        };
    }
}
