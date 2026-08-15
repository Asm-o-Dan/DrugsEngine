using Domain.Entities;

namespace Application.Interfaces;

public interface IKafkaProducer
{
    public void ProduceDrug(Drug drug);
}