public class ExitCarCommand : ICommand
{
    private readonly PlayerCarController controller;
    private readonly ParkingSpot spot;

    public ExitCarCommand(PlayerCarController controller, ParkingSpot spot)
    {
        this.controller = controller;
        this.spot = spot;
    }

    public void Execute()
    {
        if (controller == null || spot == null) return;
        controller.ExitCar(spot);
    }
}
