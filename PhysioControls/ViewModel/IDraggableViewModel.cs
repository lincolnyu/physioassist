using System.Windows;

namespace PhysioControls.ViewModel
{
    public interface IDraggableViewModel : ISelectableViewModel
    {
        Point LocationOnCanvas { get; set; }
    }
}
