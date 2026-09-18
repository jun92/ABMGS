namespace Silo.Player;

public class CellInfo
{
    public Guid PlayerId { get; set; } = Guid.Empty;
    public string MarkedTime { get; set; } = String.Empty;
    public CellState State { get; set; } = CellState.Empty;
}
