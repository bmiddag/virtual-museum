using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using VirtualMuseumAPI.Models;
using System.Security.Principal;
using Microsoft.AspNet.Identity;
using VirtualMuseumAPI.Controllers;


namespace VirtualMuseumAPI.Helpers
{
    public class VirtualMuseumFactory
    {
        VirtualMuseumDataContext dc = new VirtualMuseumDataContext();

        public VirtualMuseumFactory(VirtualMuseumDataContext context)
        {
            dc = context;
        }

        public VirtualMuseumFactory()
        {
            dc = new VirtualMuseumDataContext();
        }


        public Artwork CreateArtWork(byte[] buffer, int size, IIdentity modiByUser)
        {            
            Artwork artwork = new Artwork
            {
                ArtistID = dc.ArtistsXUsers.Where(a => a.UID == IdentityExtensions.GetUserId(modiByUser)).First().ArtistID,
                name = "",
                Collected = 0,
                ModiBy = IdentityExtensions.GetUserId(modiByUser),
                ModiDate = DateTime.Now
            };
            dc.Artworks.InsertOnSubmit(artwork);
            dc.SubmitChanges();
            ArtworkRepresentation representation = new ArtworkRepresentation
            {
                ArtworkID = artwork.ID,
                DataGUID = Guid.NewGuid(),
                Data = new System.Data.Linq.Binary(buffer),
                Size = size
            };
            dc.ArtworkRepresentations.InsertOnSubmit(representation);
            dc.SubmitChanges();
            return artwork;
        }

        public ArtworkRepresentation CreateArtWorkRepresentation(int ArtWorkID, byte[] buffer, int size, IIdentity modiByUser)
        {
            ArtworkRepresentation representation = new ArtworkRepresentation
            {
                ArtworkID = ArtWorkID,
                DataGUID = Guid.NewGuid(),
                Data = new System.Data.Linq.Binary(buffer),
                Size = size
            };
            dc.ArtworkRepresentations.InsertOnSubmit(representation);
            dc.SubmitChanges();
            return representation;
        }

        public Artist CreatePublicArtist(String name, IIdentity modiByUser, IIdentity artistUser = null)
        {
            Artist artist = new Artist
            {
                Name = name,
                ModiBy = IdentityExtensions.GetUserId(modiByUser),
                ModiDate = DateTime.Now
            };
            dc.Artists.InsertOnSubmit(artist);
            dc.SubmitChanges();
            if (artistUser != null)
            {
                ArtistsXUser artistUserEntry = new ArtistsXUser
                {
                    ArtistID = artist.ID,
                    UID = IdentityExtensions.GetUserId(artistUser)
                };
                dc.ArtistsXUsers.InsertOnSubmit(artistUserEntry);
                dc.SubmitChanges();
            }
            
            return artist;
        }

        public Artist CreatePrivateArtist(String name, String justRegisteredUserID)
        {
            if (dc.AspNetUsers.Any(a => a.Id == justRegisteredUserID))
            {
                Artist artist = new Artist
                {
                    Name = name,
                    ModiBy = justRegisteredUserID,
                    ModiDate = DateTime.Now
                };
                dc.Artists.InsertOnSubmit(artist);
                dc.SubmitChanges();

                    ArtistsXUser artistUserEntry = new ArtistsXUser
                    {
                        ArtistID = artist.ID,
                        UID = justRegisteredUserID
                    };
                    dc.ArtistsXUsers.InsertOnSubmit(artistUserEntry);
                    dc.SubmitChanges();
              
                return artist;
            }
            return null;
        }

        public Museum CreateMuseum(String name, String description, Privacy.Levels level, IIdentity ownerID, IIdentity modiByUser)
        {
            PrivacyLevel privacyLevel = dc.PrivacyLevels.Where(a=>a.Name == "PRIVATE").First();
            if (dc.PrivacyLevels.Any(a => a.Name == Enum.GetName(typeof(Privacy.Levels), (int) level))){
                privacyLevel = dc.PrivacyLevels.Where(a=>a.Name == Enum.GetName(typeof(Privacy.Levels), (int) level)).First();
            }
           
            Museum museum = new Museum()
            {
                Name = name,
                Description = description,
                PrivacyLevelID = privacyLevel.ID,
                OwnerID = IdentityExtensions.GetUserId(modiByUser),
                ModiBy = IdentityExtensions.GetUserId(modiByUser),
                Visited = 0,
                ModiDate = DateTime.Now
            };
            dc.Museums.InsertOnSubmit(museum);
            dc.SubmitChanges();
            return museum;
        }

        public PrivacyLevel CreatePrivacyLevel(String name, String description)
        {
            PrivacyLevel level = new PrivacyLevel()
            {
                Description = description,
                Name = name
            };
            dc.PrivacyLevels.InsertOnSubmit(level);
            dc.SubmitChanges();
            return level;
        }

        public ConfigValue CreateConfigValue(String setting, String value)
        {
            ConfigValue val = new ConfigValue() { Setting = setting, Value = value };
            dc.ConfigValues.InsertOnSubmit(val);
            dc.SubmitChanges();
            return val;
        }

        public ArtworkKey CreateArtworkKey(String name)
        {
            ArtworkKey key = new ArtworkKey() { name = name };
            dc.ArtworkKeys.InsertOnSubmit(key);
            dc.SubmitChanges();
            return key;
        }

        public ArtworkMetadata CreateArtworkMetadata(int artWorkID, String key, String value, IIdentity modiByUser)
        {
            int keyID = -1;
            if (dc.ArtworkKeys.Any((k => k.name.ToLower() == key.Trim().ToLower())))
            {
                keyID = dc.ArtworkKeys.First((k => k.name.ToLower() == key.Trim().ToLower())).ID;
            }
            else
            {
                keyID = CreateArtworkKey(key.ToLower().Trim()).ID;
            }

            ArtworkMetadata metadata = new ArtworkMetadata()
            {
                ArtworkID = artWorkID,
                KeyID = keyID,
                Value = value,
                ModiBy = IdentityExtensions.GetUserId(modiByUser),
                ModiDate = DateTime.Now
            };
            dc.ArtworkMetadatas.InsertOnSubmit(metadata);
            dc.SubmitChanges();
            return metadata;
        }

        public CreditsXUser CreateUserCredit(String justRegisteredUserID)
        {
            if (dc.AspNetUsers.Any(a => a.Id == justRegisteredUserID))
            {
                CreditsXUser creditUserEntry = new CreditsXUser
                {
                    Credits = 0,
                    UID = justRegisteredUserID,
                    ModiDate = DateTime.Now
                };
                dc.CreditsXUsers.InsertOnSubmit(creditUserEntry);
                dc.SubmitChanges();

                return creditUserEntry;
            }
            return null;
        }

        public CreditAction CreateCreditAction(String name, String description, int credits)
        {
            CreditAction action = new CreditAction
            {
                Name = name,
                Description = description,
                Credits = credits
            };
            dc.CreditActions.InsertOnSubmit(action);
            dc.SubmitChanges();
            return action;
        }

       
        public ArtworkFilter CreateArtWorkFilter(int artistID, IIdentity modiByUser, int artWorkID = -1)
        {
            ArtworkFilter filter = new ArtworkFilter()
            {
                ArtistID = artistID,
                ModiBy = modiByUser.GetUserId(),
                ModiDate = DateTime.Now
            };
            if (artWorkID != -1)
            {
                filter.ArtworkID = artWorkID;
            }
            dc.ArtworkFilters.InsertOnSubmit(filter);
            dc.SubmitChanges();
            return filter;
        }

        public ArtworkFilterValue CreateArtWorkFilterValue(int artworkFilterID, int artworkKeyID, string value)
        {
            ArtworkFilterValue artworkFiltervalue = new ArtworkFilterValue()
            {
                ArtworkFilterID = artworkFilterID,
                ArtworkKeyID = artworkKeyID,
                Value = value
            };
            dc.ArtworkFilterValues.InsertOnSubmit(artworkFiltervalue);
            dc.SubmitChanges();
            return artworkFiltervalue;
        }

        public ArtworkFiltersXUser AssignArtWorkFilterToUser(int artWorkFilterID, IIdentity user)
        {
            if (!dc.ArtworkFiltersXUsers.Any(a => a.ArtworkFilterID == artWorkFilterID && a.AspNetUser.Id == user.GetUserId()))
            {
                ArtworkFiltersXUser assignToUser = new ArtworkFiltersXUser()
                {
                    ArtworkFilterID = artWorkFilterID,
                    UID = user.GetUserId()
                };
                dc.ArtworkFiltersXUsers.InsertOnSubmit(assignToUser);
                dc.SubmitChanges();
                return assignToUser;

            }
            else
            {
                return dc.ArtworkFiltersXUsers.First(a => a.ArtworkFilterID == artWorkFilterID && a.AspNetUser.Id == user.GetUserId());
            }
            
        }
    }
 
}