using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Config;

public class RoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    private const string AdminRoleId = "3071ff28-a856-4bff-8d16-65a729dbf3c0";
    private const string CustomerRoleId = "1eeb7c27-c590-44ec-b013-2b97b1e95802";

    public void Configure(EntityTypeBuilder<IdentityRole> builder)
    {
        builder.HasData(
            new IdentityRole { Id = AdminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole { Id = CustomerRoleId, Name = "Customer", NormalizedName = "CUSTOMER" }
        );
    }
}
