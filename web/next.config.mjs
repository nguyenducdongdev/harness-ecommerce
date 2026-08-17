/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  // Ảnh sản phẩm Phase 2 sẽ serve từ MinIO/CDN — cấu hình remotePatterns khi có domain
  images: {
    remotePatterns: [
      { protocol: "http", hostname: "localhost", port: "9000" },
      { protocol: "https", hostname: "**" },
    ],
  },
  async rewrites() {
    // Proxy /api sang backend .NET trong dev — tránh CORS
    return [
      {
        source: "/api/:path*",
        destination: `${process.env.API_URL ?? "http://localhost:5080"}/api/:path*`,
      },
    ];
  },
};

export default nextConfig;
