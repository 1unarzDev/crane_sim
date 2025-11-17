using Sim.Utils.ROS;

namespace Sim.Sensors {
    public abstract class ROSSensorBase<T> : ROSPublisherBase<T> where T : Unity.Robotics.ROSTCPConnector.MessageGeneration.Message {
        protected override void SetDefaults() {
            SetSensorDefaults();
        }

        protected abstract void SetSensorDefaults();


        protected override T CreateMessage() {
            return CreateSensorMessage();
        }
        protected abstract T CreateSensorMessage();
    }
}
