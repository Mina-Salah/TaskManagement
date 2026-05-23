using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;


namespace TaskManagement.Domain.Interfaces
{
	public interface IUnitOfWork
	{
		IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;
		Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
	}
}
