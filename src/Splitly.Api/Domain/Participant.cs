namespace Splitly.Api.Domain;

public sealed class Participant
{
    public Guid Id { get; private set; }

    public string Name { get; private set; }

    internal Participant(Guid id, string name)
    {
        Id = id;
        Name = name;
    }
}
