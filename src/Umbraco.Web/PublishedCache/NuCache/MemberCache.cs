﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;
using Umbraco.Core;
using Umbraco.Core.Cache;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Security;
using Umbraco.Core.Services;
using Umbraco.Core.Xml.XPath;
using Umbraco.Web.PublishedCache.NuCache.Navigable;

namespace Umbraco.Web.PublishedCache.NuCache
{
    class MemberCache : IPublishedMemberCache, INavigableData
    {
        private readonly IMemberService _memberService;
        private readonly IDataTypeService _dataTypeService;
        private readonly PublishedContentTypeCache _contentTypeCache;
        private readonly bool _previewDefault;

        public MemberCache(bool previewDefault, IMemberService memberService, IDataTypeService dataTypeService, PublishedContentTypeCache contentTypeCache)
        {
            _memberService = memberService;
            _dataTypeService = dataTypeService;
            _contentTypeCache = contentTypeCache;
            _previewDefault = previewDefault;
        }

        //private static T GetCacheItem<T>(string cacheKey)
        //    where T : class
        //{
        //    var facade = Facade.Current;
        //    var cache = facade == null ? null : facade.FacadeCache;
        //    return cache == null
        //        ? null
        //        : (T) cache.GetCacheItem(cacheKey);
        //}

        private static T GetCacheItem<T>(string cacheKey, Func<T> getCacheItem)
            where T : class
        {
            var facade = Facade.Current;
            var cache = facade == null ? null : facade.FacadeCache;
            return cache == null
                ? getCacheItem()
                : cache.GetCacheItem<T>(cacheKey, getCacheItem);
        }

        private static void EnsureProvider()
        {
            var provider = Core.Security.MembershipProviderExtensions.GetMembersMembershipProvider();
            if (provider.IsUmbracoMembershipProvider() == false)
                throw new NotSupportedException("Cannot access this method unless the Umbraco membership provider is active");
        }

        public IPublishedContent GetById(bool preview, int memberId)
        {
            return GetById(memberId);
        }

        public IPublishedContent /*IPublishedMember*/ GetById(int memberId)
        {
            return GetCacheItem(CacheKeys.MemberCacheMember("ById", _previewDefault, memberId), () =>
                {
                    EnsureProvider();
                    var member = _memberService.GetById(memberId);
                    return member == null 
                        ? null 
                        : PublishedMember.Create(member, GetContentType(member.ContentTypeId), _previewDefault);
                });
        }

        private IPublishedContent /*IPublishedMember*/ GetById(IMember member, bool previewing)
        {
            return GetCacheItem(CacheKeys.MemberCacheMember("ById", _previewDefault, member.Id), () =>
                PublishedMember.Create(member, GetContentType(member.ContentTypeId), previewing));
        }

        public IPublishedContent /*IPublishedMember*/ GetByProviderKey(object key)
        {
            return GetCacheItem(CacheKeys.MemberCacheMember("ByProviderKey", _previewDefault, key), () =>
                {
                    EnsureProvider();
                    var member = _memberService.GetByProviderKey(key);
                    return member == null ? null : GetById(member, _previewDefault);
                });
        }

        public IPublishedContent /*IPublishedMember*/ GetByUsername(string username)
        {
            return GetCacheItem(CacheKeys.MemberCacheMember("ByUsername", _previewDefault, username), () =>
                {
                    EnsureProvider();
                    var member = _memberService.GetByUsername(username);
                    return member == null ? null : GetById(member, _previewDefault);
                });
        }

        public IPublishedContent /*IPublishedMember*/ GetByEmail(string email)
        {
            return GetCacheItem(CacheKeys.MemberCacheMember("ByEmail", _previewDefault, email), () =>
                {
                    EnsureProvider();
                    var member = _memberService.GetByEmail(email);
                    return member == null ? null : GetById(member, _previewDefault);
                });
        }

        public IPublishedContent /*IPublishedMember*/ GetByMember(IMember member)
        {
            return PublishedMember.Create(member, GetContentType(member.ContentTypeId), _previewDefault);
        }

        public IEnumerable<IPublishedContent> GetAtRoot(bool preview)
        {
            // because members are flat (not a tree) everything is at root
            // because we're loading everything... let's just not cache?
            var members = _memberService.GetAllMembers();
            return members.Select(m => PublishedMember.Create(m, GetContentType(m.ContentTypeId), preview));
        }

        public XPathNavigator CreateNavigator()
        {
            var source = new Source(this, false);
            var navigator = new NavigableNavigator(source);
            return navigator;
        }

        public XPathNavigator CreateNavigator(bool preview)
        {
            return CreateNavigator();
        }

        public XPathNavigator CreateNodeNavigator(int id, bool preview)
        {
            var provider = Core.Security.MembershipProviderExtensions.GetMembersMembershipProvider();
            if (provider.IsUmbracoMembershipProvider() == false)
            {
                throw new NotSupportedException("Cannot access this method unless the Umbraco membership provider is active");
            }

            var result = _memberService.GetById(id);
            if (result == null) return null;

            var exs = new EntityXmlSerializer();
            var s = exs.Serialize(_dataTypeService, result);
            var n = s.GetXmlNode();
            return n.CreateNavigator();
        }

        #region Content types

        public PublishedContentType GetContentType(int id)
        {
            return _contentTypeCache.Get(PublishedItemType.Member, id);
        }

        public PublishedContentType GetContentType(string alias)
        {
            return _contentTypeCache.Get(PublishedItemType.Member, alias);
        }

        #endregion
    }
}
