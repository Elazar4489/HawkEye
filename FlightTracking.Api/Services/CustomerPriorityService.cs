using FlightTracking.Api.DTOs;
using FlightTracking.Api.Models;
using FlightTracking.Api.Repositories;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace FlightTracking.Api.Services
{
    public class CustomerPriorityService : ICustomerPriorityService
    {
        private readonly ICustomerPriorityRepository _repository;
        // יצירת Factory גיאומטרי עם SRID 4326 (מערכת קואורדינטות GPS עולמית)
        private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        public CustomerPriorityService(ICustomerPriorityRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<CustomerPriorityResponseDto>> GetUserPrioritiesAsync(int userId)
        {
            var priorities = await _repository.GetByUserIdAsync(userId);
            return priorities.Select(MapToDto);
        }

        public async Task<CustomerPriorityResponseDto> CreatePriorityAsync(CreateCustomerPriorityDto dto)
        {
            var entity = new CustomerPriority
            {
                UserId = dto.UserId,
                Priority = dto.Priority,
                AreaType = dto.AreaType.ToUpper(),
                RadiusMeters = dto.RadiusMeters
            };

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                // יצירת נקודת מרכז (X = קו אורך, Y = קו רוחב)
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                // פוליגון חייב להיות סגור - אם הנקודה האחרונה שונה מהראשונה, נוסיף אותה לסוף
                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            await _repository.AddAsync(entity);
            await _repository.SaveChangesAsync();

            return MapToDto(entity);
        }
        public async Task<bool> UpdatePriorityAsync(int id, UpdateCustomerPriorityDto dto)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            entity.Priority = dto.Priority;
            entity.AreaType = dto.AreaType.ToUpper();
            entity.RadiusMeters = dto.RadiusMeters;

            // איפוס גיאומטריות ישנות כדי לבנות את החדשות נכון
            entity.CenterPoint = null;
            entity.GeometryData = null;

            if (entity.AreaType == "RADIUS" && dto.CenterLongitude.HasValue && dto.CenterLatitude.HasValue)
            {
                entity.CenterPoint = _geometryFactory.CreatePoint(new Coordinate(dto.CenterLongitude.Value, dto.CenterLatitude.Value));
            }
            else if (entity.AreaType == "POLYGON" && dto.PolygonCoordinates != null && dto.PolygonCoordinates.Count >= 3)
            {
                var coordinates = dto.PolygonCoordinates.Select(c => new Coordinate(c.Longitude, c.Latitude)).ToList();

                if (coordinates.First().X != coordinates.Last().X || coordinates.First().Y != coordinates.Last().Y)
                {
                    coordinates.Add(coordinates.First());
                }

                entity.GeometryData = _geometryFactory.CreatePolygon(coordinates.ToArray());
            }

            // ה-Repository כבר מנהל את ה-Update של ה-Entity, נשאר רק לשמור שינויים
            return await _repository.SaveChangesAsync();
        }

        public async Task<bool> DeletePriorityAsync(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            await _repository.DeleteAsync(id);
            return await _repository.SaveChangesAsync();
        }

        // פונקציית עזר להמרת Entity ל-DTO חזרה
        private static CustomerPriorityResponseDto MapToDto(CustomerPriority entity)
        {
            var dto = new CustomerPriorityResponseDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Priority = entity.Priority,
                AreaType = entity.AreaType,
                RadiusMeters = entity.RadiusMeters
            };

            if (entity.CenterPoint != null)
            {
                dto.CenterLongitude = entity.CenterPoint.X;
                dto.CenterLatitude = entity.CenterPoint.Y;
            }

            if (entity.GeometryData is Polygon polygon)
            {
                dto.PolygonCoordinates = polygon.Coordinates
                    .Select(c => new CoordinateDto { Longitude = c.X, Latitude = c.Y })
                    .ToList();
            }

            return dto;
        }
    }
}