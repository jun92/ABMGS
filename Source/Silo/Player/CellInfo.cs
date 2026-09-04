namespace Silo.Player;

public class CellInfo
{
    public Guid PlayerId { get; set; } = Guid.Empty;
    public DateTime MarkedTime { get; set; } = DateTime.MinValue;
    public CellState State { get; set; } = CellState.Empty;
}
