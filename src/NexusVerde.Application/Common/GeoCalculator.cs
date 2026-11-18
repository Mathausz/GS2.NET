namespace NexusVerde.Application.Common;

public static class GeoCalculator
{
    private const double RaioTerraKm = 6371.0088;

    /// <summary>Distância em km entre dois pontos (fórmula de Haversine).</summary>
    public static double DistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ParaRadianos(lat2 - lat1);
        var dLon = ParaRadianos(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ParaRadianos(lat1)) * Math.Cos(ParaRadianos(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return RaioTerraKm * c;
    }

    private static double ParaRadianos(double graus) => graus * Math.PI / 180.0;
}
