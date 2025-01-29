namespace PortaleFatture.BE.SelfCareOnBoardingAPI.Dto; 
 
public class User
{
    public string? TaxCode { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
}

public class GeographicTaxonomy
{
    public string? Code { get; set; }
    public string? Desc { get; set; }
}

public class Aggregate
{
    public string? TaxCode { get; set; }
    public string? Description { get; set; }
    public string? SubunitCode { get; set; }
    public string? SubunitType { get; set; }
    public string? VatNumber { get; set; }
    public string? ParentDescription { get; set; }
    public List<GeographicTaxonomy>? GeographicTaxonomies { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public string? OriginId { get; set; }
    public string? Origin { get; set; }
    public List<User>? Users { get; set; }
    public string? RecipientCode { get; set; }
    public string? DigitalAddress { get; set; }
    public string? City { get; set; }
    public string? County { get; set; }
    public string? TaxCodePT { get; set; }
    public string? Iban { get; set; }
    public string? Service { get; set; }
    public string? SyncAsyncMode { get; set; }
}

public class Institution
{
    public string? InstitutionType { get; set; }
    public string? TaxCode { get; set; }
    public string? SubunitCode { get; set; }
    public string? SubunitType { get; set; }
    public string? Origin { get; set; }
    public string? OriginId { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? County { get; set; }
    public string? Description { get; set; }
    public string? DigitalAddress { get; set; }
    public string? Address { get; set; }
    public string? ZipCode { get; set; }
    public List<GeographicTaxonomy>? GeographicTaxonomies { get; set; }
    public string? Rea { get; set; }
    public string? ShareCapital { get; set; }
    public string? BusinessRegisterPlace { get; set; }
    public string? SupportEmail { get; set; }
    public string? SupportPhone { get; set; }
    public bool Imported { get; set; }
}

public class Billing
{
    public string? VatNumber { get; set; }
    public string? RecipientCode { get; set; }
    public bool PublicServices { get; set; }
}

public class AdditionalInformations
{
    public bool BelongRegulatedMarket { get; set; }
    public string? RegulatedMarketNote { get; set; }
    public bool Ipa { get; set; }
    public string? IpaCode { get; set; }
    public bool EstablishedByRegulatoryProvision { get; set; }
    public string? EstablishedByRegulatoryProvisionNote { get; set; }
    public bool AgentOfPublicService { get; set; }
    public string? AgentOfPublicServiceNote { get; set; }
    public string? OtherNote { get; set; }
}

public class OnboardingUpdateRequest
{
    public string? ProductId { get; set; }
    public List<User>? Users { get; set; }
    public List<Aggregate>? Aggregates { get; set; }
    public bool IsAggregator { get; set; }
    public string? PricingPlan { get; set; }
    public bool SignContract { get; set; }
    public Institution? Institution { get; set; }
    public Billing? Billing { get; set; }
    public AdditionalInformations? AdditionalInformations { get; set; }
}