using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using enInvBackEnd.DataModels;

namespace enInvBackEnd.DataContext;

public partial class EninvContext : DbContext
{
    public EninvContext()
    {
    }

    public EninvContext(DbContextOptions<EninvContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CompanyDetail> CompanyDetails { get; set; }

    public virtual DbSet<CompanyUser> CompanyUsers { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<LhdnProfile> LhdnProfiles { get; set; }

    public virtual DbSet<LhdnToken> LhdnTokens { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserType> UserTypes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseNpgsql("Host=37.27.87.165;port=7965;Database=eninv;Username=postgres;Password=Z@m!nt3rn@t!0n@l; Pooling=true; Timeout=300");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CompanyDetail>(entity =>
        {
            entity.HasKey(e => e.CompanyId).HasName("Company_details_pkey");

            entity.ToTable("Company_details");

            entity.Property(e => e.CompanyId)
                .ValueGeneratedNever()
                .HasColumnName("CompanyID");
            entity.Property(e => e.AddressL1).HasColumnType("character varying");
            entity.Property(e => e.AddressL2).HasColumnType("character varying");
            entity.Property(e => e.City)
                .HasColumnType("character varying")
                .HasColumnName("city");
            entity.Property(e => e.CompanyName).HasColumnType("character varying");
            entity.Property(e => e.ContectNumber)
                .HasColumnType("character varying")
                .HasColumnName("contectNumber");
            entity.Property(e => e.Country)
                .HasColumnType("character varying")
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasColumnType("character varying")
                .HasColumnName("email");
            entity.Property(e => e.IdNumber)
                .HasColumnType("character varying")
                .HasColumnName("idNumber");
            entity.Property(e => e.IdType)
                .HasColumnType("character varying")
                .HasColumnName("idType");
            entity.Property(e => e.MsicCode)
                .HasColumnType("character varying")
                .HasColumnName("msic_code");
            entity.Property(e => e.PostCode).HasColumnName("post_code");
            entity.Property(e => e.SstNumber)
                .HasColumnType("character varying")
                .HasColumnName("sstNumber");
            entity.Property(e => e.State)
                .HasColumnType("character varying")
                .HasColumnName("state");
            entity.Property(e => e.StateCode)
                .HasColumnType("character varying")
                .HasColumnName("state_code");
            entity.Property(e => e.Taxid)
                .HasColumnType("character varying")
                .HasColumnName("taxid");
            entity.Property(e => e.TourismTaxNo)
                .HasColumnType("character varying")
                .HasColumnName("tourism_tax_no");
        });

        modelBuilder.Entity<CompanyUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CompanyUsers_pkey");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("customer_pkey");

            entity.ToTable("customer");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.Brn)
                .HasMaxLength(50)
                .HasColumnName("brn");
            entity.Property(e => e.CityName)
                .HasMaxLength(100)
                .HasColumnName("city_name");
            entity.Property(e => e.CompanyId)
                .HasMaxLength(50)
                .HasColumnName("company_id");
            entity.Property(e => e.CompanyName)
                .HasMaxLength(255)
                .HasColumnName("company_name");
            entity.Property(e => e.CountryCode)
                .HasMaxLength(10)
                .HasColumnName("country_code");
            entity.Property(e => e.CountrySubentityCodeStateCode)
                .HasMaxLength(10)
                .HasDefaultValueSql("NULL::character varying")
                .HasColumnName("country_subentity_code_state_code");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasDefaultValueSql("NULL::character varying")
                .HasColumnName("email");
            entity.Property(e => e.PostalZone)
                .HasMaxLength(10)
                .HasColumnName("postal_zone");
            entity.Property(e => e.Sst)
                .HasMaxLength(50)
                .HasColumnName("sst");
            entity.Property(e => e.Telephone)
                .HasMaxLength(20)
                .HasColumnName("telephone");
            entity.Property(e => e.Tin)
                .HasMaxLength(50)
                .HasColumnName("tin");
            entity.Property(e => e.Ttx)
                .HasMaxLength(50)
                .HasColumnName("ttx");
            entity.Property(e => e.UserId).HasColumnName("userId");
        });

        modelBuilder.Entity<LhdnProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("LhdnProfile_pkey");

            entity.ToTable("LhdnProfile");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ClientIdLhdn)
                .HasColumnType("character varying")
                .HasColumnName("client_id_lhdn");
            entity.Property(e => e.ClientSecretLhdn)
                .HasColumnType("character varying")
                .HasColumnName("client_secret_lhdn");
            entity.Property(e => e.CompanyId).HasColumnName("CompanyID");
            entity.Property(e => e.GrantTypeLhdn)
                .HasDefaultValueSql("'client_credentials'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("grant_type_lhdn");
            entity.Property(e => e.IntrigrationType)
                .HasDefaultValueSql("'sandbox'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("intrigration_type");
            entity.Property(e => e.ScopeLhdn)
                .HasDefaultValueSql("'InvoicingAPI'::character varying")
                .HasColumnType("character varying")
                .HasColumnName("scope_lhdn");
        });

        modelBuilder.Entity<LhdnToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("LhdnToken_pkey");

            entity.ToTable("LhdnToken");

            entity.Property(e => e.TokenId)
                .ValueGeneratedNever()
                .HasColumnName("token_id");
            entity.Property(e => e.CompanyId).HasColumnName("company_id");
            entity.Property(e => e.ExpieryDateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("expiery_date_time");
            entity.Property(e => e.IssueddateTime)
                .HasColumnType("timestamp without time zone")
                .HasColumnName("issueddate_time");
            entity.Property(e => e.Token).HasColumnName("token");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Nname).HasColumnType("character varying");
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.PhoneNo)
                .HasColumnType("character varying")
                .HasColumnName("phone_no");
            entity.Property(e => e.Roll).HasColumnType("character varying");
        });

        modelBuilder.Entity<UserType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("UserType_pkey");

            entity.ToTable("UserType");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasColumnType("character varying")
                .HasColumnName("type");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
