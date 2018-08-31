namespace LichessApi
{
    using LichessApi.Models;

    public interface ILichessApiClient
    {
        DatabasePosition GetPositionInfo(string fen);

        TablebasePosition GetTablebaseInfo(string fen);
    }
}
