namespace ListeDeCourses.Api.DTOs
{
    public static class DtoConstraints
    {
        public const int NameMin = 1;
        public const int NameMax = 120;

        public const int IngredientNameMax = 100;
        public const int AisleMax = 100;

        public const int UnitMax = 50;

        public const int EmailMax = 255;
        public const int PseudoMin = 2;
        public const int PseudoMax = 50;

        public const int PasswordMin = 8;
        public const int PasswordMax = 100;
    }
}
