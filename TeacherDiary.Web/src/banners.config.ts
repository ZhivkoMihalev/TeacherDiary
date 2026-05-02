export interface BannerConfig {
  imageUrl: string
  linkUrl: string
}

// Add or remove banners here. Each banner must have an image URL and a link URL.
import bannerNarval from './assets/banner-right.png'

export const BANNERS: BannerConfig[] = [
  {
    imageUrl: bannerNarval,
    linkUrl: '#',
  },
]
