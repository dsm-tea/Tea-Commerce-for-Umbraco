﻿using Autofac;
using TeaCommerce.Api.Common;
using TeaCommerce.Api.Dependency;
using TeaCommerce.Api.InformationExtractors;
using TeaCommerce.Api.Models;
using TeaCommerce.Api.PriceCalculators;
using TeaCommerce.Api.Services;
using TeaCommerce.Umbraco.Configuration.InformationExtractors;
using Umbraco.Core.Models;

namespace TeaCommerce.Umbraco.Configuration.PriceCalculators {

  [SuppressDependency( "TeaCommerce.Api.PriceCalculators.IProductCalculator`2[[Umbraco.Core.Models.IPublishedContent, Umbraco.Core],[System.String, mscorlib]]", "TeaCommerce.Api" )]
  public class PublishedContentProductCalculator : IProductCalculator<IPublishedContent, string> {

    protected readonly IVatGroupService VatGroupService;
    protected readonly IPublishedContentProductInformationExtractor ProductInformationExtractor;

    public PublishedContentProductCalculator( IVatGroupService vatGroupService, IPublishedContentProductInformationExtractor productInformationExtractor ) {
      VatGroupService = vatGroupService;
      ProductInformationExtractor = productInformationExtractor;
    }

    #region With order

    public virtual VatRate CalculateVatRate( IPublishedContent product, string variantId, Order order ) {
      order.MustNotBeNull( "order" );

      return CalculateVatRate( product, variantId, order.PaymentInformation.CountryId, order.PaymentInformation.CountryRegionId, order.ShipmentInformation.CountryId, order.ShipmentInformation.CountryRegionId, order.VatRate.Copy() );
    }

    public virtual Price CalculatePrice( IPublishedContent product, string variantId, Currency currency, VatRate vatRate, Order order ) {
      order.MustNotBeNull( "order" );
      vatRate.MustNotBeNull( "vatRate" );

      return CalculatePrice( product, variantId, currency, vatRate );
    }

    #endregion

    #region Without order

    public virtual VatRate CalculateVatRate( IPublishedContent product, string variantId, long paymentCountryId, long? paymentCountryRegionId, long? shippingCountryId, long? shippingCountryRegionId, VatRate fallbackVatRate ) {
      fallbackVatRate.MustNotBeNull( "fallbackVatRate" );

      VatRate vatRate = fallbackVatRate;

      long? vatGroupId = ProductInformationExtractor.GetVatGroupId( product, variantId );
      if ( vatGroupId != null ) {
        vatRate = VatGroupService.Get( ProductInformationExtractor.GetStoreId( product ), vatGroupId.Value ).GetVatRate( paymentCountryId, paymentCountryRegionId, shippingCountryId, shippingCountryRegionId );
      }

      return vatRate;
    }

    public virtual Price CalculatePrice( IPublishedContent product, string variantId, Currency currency, VatRate vatRate ) {
      currency.MustNotBeNull( "currency" );
      vatRate.MustNotBeNull( "vatRate" );

      OriginalUnitPrice originalUnitPrice = ProductInformationExtractor.GetOriginalUnitPrices( product, variantId ).Get( currency.Id ) ?? new OriginalUnitPrice( 0M, currency.Id );

      return new Price( originalUnitPrice.Value, vatRate, currency );
    }

    #endregion

  }
}
