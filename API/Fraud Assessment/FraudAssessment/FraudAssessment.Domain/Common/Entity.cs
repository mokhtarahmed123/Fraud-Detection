namespace FraudAssessment.Domain.Common;
public abstract class Entity
{
    private readonly List<object> _domainEvents = new();
    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public Guid Id { get; protected set; }

    protected Entity()
    {
        Id = Guid.NewGuid();
    }
    protected void RaiseDomainEvent(object domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}