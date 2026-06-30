namespace HungNT.UI.Panel
{
    /// <summary>
    /// Panel nhận dữ liệu khởi tạo. UIPanelManager gọi SetData ở trạng thái inactive,
    /// trước khi panel được active (trước Awake/OnEnable/OnShow).
    /// </summary>
    public interface IPanelData<in TData>
    {
        /// <summary>
        /// Nhận dữ liệu khởi tạo. Chạy trước khi GameObject active.
        /// </summary>
        void SetData(TData data);
    }
}
