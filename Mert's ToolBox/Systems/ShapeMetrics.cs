using Unity.Mathematics;

namespace MertsToolBox.Systems
{
    public readonly struct ShapeMetrics
    {
        public readonly int OuterDimensionMeters;
        public readonly int CenterDimensionMeters;
        public readonly int InnerDimensionMeters;

        public readonly float OuterDimensionUnits;
        public readonly float InnerDimensionUnits;

        private ShapeMetrics(
            int outerDimensionMeters,
            int centerDimensionMeters,
            int innerDimensionMeters,
            float outerDimensionUnits,
            float innerDimensionUnits)
        {
            OuterDimensionMeters = outerDimensionMeters;
            CenterDimensionMeters = centerDimensionMeters;
            InnerDimensionMeters = innerDimensionMeters;

            OuterDimensionUnits = outerDimensionUnits;
            InnerDimensionUnits = innerDimensionUnits;
        }

        public static ShapeMetrics FromOuterDimension(int outerDimensionMeters, float roadWidth)
        {
            int widthMeters = (int)math.round(roadWidth);

            int outer = math.max(0, outerDimensionMeters);
            int center = math.max(0, outer - widthMeters);
            int inner = math.max(0, outer - (widthMeters * 2));

            float outerU = outer / 8f;
            float innerU = inner / 8f;

            return new ShapeMetrics(
                outer,
                center,
                inner,
                outerU,
                innerU
            );
        }

    }
}