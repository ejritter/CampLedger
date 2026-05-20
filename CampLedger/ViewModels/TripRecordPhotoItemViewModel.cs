using System.IO;

namespace CampLedger.ViewModels;

public sealed class TripRecordPhotoItemViewModel
{
    private readonly byte[] _photoData;

    public TripRecordPhotoItemViewModel(string name, byte[] photoData)
    {
        Name = name;
        _photoData = photoData;
    }

    public string Name { get; }

    public ImageSource PhotoSource
    {
        get
        {
            return ImageSource.FromStream(() => new MemoryStream(_photoData));
        }
    }
}