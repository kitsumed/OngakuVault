---
# https://vitepress.dev/reference/default-theme-home-page
layout: home

hero:
  name: "Ongakuvault"
  text: "Effortlessly archive audio from almost all website, Anytime!!"
  tagline: Never lose access to your favorites audio again!
  actions:
    - theme: brand
      text: Getting Started
      link: /getting-started
    - theme: alt
      text: Showcase
      link: /getting-started#showcase
    - theme: alt
      text: See on github
      link: https://github.com/kitsumed/OngakuVault

features:
  - title: Support for a Wide Range of Websites
    details: Powered by the yt-dlp library, enabling seamless downloads from a variety of website.
    link: /getting-started#what-website-are-supported
    linkText: Learn more
  - title: Simultaneous Downloads
    details: Take control with the ability to configure how many downloads can run concurrently, optimizing your time.
    link: /configurations
    linkText: See available configurations
  - title: Include a Web Interface
    details: Initiate download requests easily via an optional website interface that connects to the API—no need for desktop installations or mobile apps.
    link: /getting-started#showcase
    linkText: See web interface showcase
  - title: Preserve Audio Metadata
    details: By default, the original audio metadata is kept, but any field you provide will overwrite the existing data, enabling you to effortlessly manage the metadata of your downloaded audio.
---
