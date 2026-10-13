"""EcoSort AI backend package (copy this directory as ai_model)."""
if __package__:
    from .inference import predict_image
else:
    from inference import predict_image

__all__ = ["predict_image"]
