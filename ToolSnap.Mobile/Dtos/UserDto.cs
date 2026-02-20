namespace ToolSnap.Mobile.Dtos
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public bool ConfirmedEmail { get; set; }
        public Guid RoleId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
