---
# Feel free to add content and custom Front Matter to this file.
# To modify the layout, see https://jekyllrb.com/docs/themes/#overriding-theme-defaults

layout: home
title: Contents and Introduction
nav_order: 0.0
---

**Hello!** This is the online handbook for Dylan Beattie's workshop <a href="https://ursatile.com/workshops/intro-to-distributed-systems-dotnet.html">Distributed Systems with .NET</a>.

<ul id="index-nav">
    {% assign contents = site.pages | where_exp:"item", "item.summary != nil" %}
    {% for page in contents %}
    <li>
        <a href="{{ page.url | relative_url }}">{{ page.title }}</a>
        <p>{{ page.summary }}</p>
    </li>
    {% endfor %}
</ul>
