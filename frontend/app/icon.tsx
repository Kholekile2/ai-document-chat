import { ImageResponse } from "next/og";

export const size = {
  width: 32,
  height: 32,
};

export const contentType = "image/png";

export default function Icon() {
  return new ImageResponse(
    (
      <div
        style={{
          alignItems: "center",
          background: "linear-gradient(135deg, #0f766e 0%, #0b1020 100%)",
          color: "#ffffff",
          display: "flex",
          fontFamily: "Arial, sans-serif",
          fontSize: 18,
          fontWeight: 700,
          height: "100%",
          justifyContent: "center",
          letterSpacing: -0.5,
          width: "100%",
        }}
      >
        AI
      </div>
    ),
    {
      ...size,
    }
  );
}
