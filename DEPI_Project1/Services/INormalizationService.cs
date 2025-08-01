namespace DEPI_Project1.Services
{
    public interface INormalizationService
    {
        void NormalizeEntity<T>(T entity);
    }
}