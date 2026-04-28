
using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LewisZhang_Assignment4
{
	    public class PhysicsEntity : SpriteEntity
	    {
	        protected float friction = 0.90f;
	        protected float maxSpeedX = 5f;
	        protected float maxRiseSpeed = 50f;
	        protected float maxFallSpeed = 50f;
	        protected Vector2 gravity = new Vector2(0, 1f);

        protected Boolean useGravity = true;
        protected Boolean isGrounded = false;
        protected Vector2 velocity = Vector2.Zero;
        protected Vector2 acceleration = Vector2.Zero;

        public PhysicsEntity(Art art, Vector2 position) : base(art, position)
        {
            _texture = AssetManager.GetTexture(art);
        }

	        public override void Update(GameTime gameTime)
	        {
	            if (useGravity) { acceleration += gravity; }
	            velocity += acceleration;
	            velocity.X = MathHelper.Clamp(velocity.X, -maxSpeedX, maxSpeedX);
	            velocity.Y = MathHelper.Clamp(velocity.Y, -maxRiseSpeed, maxFallSpeed);
	            velocity = _scene.CheckForGridCollision(this, velocity);

	            _position += velocity;
	            // Translate(velocity);    
	            if (useGravity) { isGrounded = IsTouchingGround(); }
	            velocity.X *= friction;
	        }

        bool IsTouchingGround()
        {
			if (_scene == null || _scene.Grid == null)
			{
				return false;
			}

			if (velocity.Y < 0f)
			{
				return false;
			}

			Rectangle bounds = GetBounds();
			if (bounds == Rectangle.Empty)
			{
				return false;
			}

			int footY = bounds.Bottom + 1;
			int inset = Math.Max(2, bounds.Width / 5);
			int leftX = bounds.Left + inset;
			int rightX = bounds.Right - 1 - inset;
			int midX = (leftX + rightX) / 2;

			return _scene.Grid.IsTileSolid(new Vector2(leftX, footY))
				   || _scene.Grid.IsTileSolid(new Vector2(midX, footY))
				   || _scene.Grid.IsTileSolid(new Vector2(rightX, footY));
        }

		protected void SetPhysicsTuning(
			float newFriction,
			float newMaxSpeedX,
			float newMaxRiseSpeed,
			float newMaxFallSpeed,
			Vector2 newGravity)
		{
			friction = newFriction;
			maxSpeedX = newMaxSpeedX;
			maxRiseSpeed = newMaxRiseSpeed;
			maxFallSpeed = newMaxFallSpeed;
			gravity = newGravity;
		}
    }
}
