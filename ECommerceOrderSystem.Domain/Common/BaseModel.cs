namespace ECommerceOrderSystem.Domain.Common
{
    public class BaseModel
    {
        public Guid Id { get; set; } = Guid.NewGuid();
    }
}
