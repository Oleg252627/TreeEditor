namespace TreeEditor.Application.Trees.Reset;

public interface ITreeDatabaseResetter
{
    Task ResetAsync(CancellationToken cancellationToken = default);
}