import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  title: "Ongakuvault",
  lang: "en-ca",
  description: "The Ongakuvault documentation website!",
  base: '/ongakuvault/', // We overwrite the root "/" base for "/repo-name/" due to how github pages work
  sitemap: { // Optional, create a sitemap.xml file
    hostname: "https://kitsumed.github.io/ongakuvault/",
  },
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: 'Home', link: '/' },
      { text: 'Getting Started', link: '/getting-started' }
    ],

    sidebar: [
      {
        text: 'Documentation',
        items: [
          { text: 'Getting started', link: '/getting-started' },
          { text: 'Installation', link: '/installation' },
          {
            text: 'Configurations', link: '/configurations',
            items: [
              { text: 'Environment Variables', link: '/configurations/environment-variables' },
              { text: 'Replacing third-party binary', link: '/configurations/replacing-third-party-binary' },
            ]
          },
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/kitsumed/OngakuVault' }
    ],

    search: {
      provider: 'local',
      options: {
        detailedView: true,
      }
    },

    footer: {
      message: 'Released under the <a href="https://www.apache.org/licenses/LICENSE-2.0">Apache 2.0</a> license',
      copyright: 'Copyright © 2025-present <a href="https://github.com/kitsumed">kitsumed (Med)</a>'
    },

    editLink: {
      pattern: 'https://github.com/kitsumed/ongakuvault/edit/main/docs/:path',
      text: 'Edit this page on GitHub'
    }

  },
  markdown: {
    lineNumbers: true,
  },
  lastUpdated: true,
})
