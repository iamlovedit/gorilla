import React from "react";
import { Layout, type FooterProps } from "@lobehub/ui";
import { Header } from "@lobehub/ui";
import { Footer } from "@lobehub/ui";

const columns: FooterProps["columns"] = [
  {
    items: [
      {
        description: "AIGC Components",
        openExternal: true,
        title: "🤯 Lobe UI",
        url: "https://github.com/lobehub/lobe-ui",
      },
      {
        description: "Chatbot Client",
        openExternal: true,
        title: "🤯 Lobe Chat",
        url: "https://github.com/lobehub/lobe-chat",
      },
      {
        description: "Node Flow Editor",
        openExternal: true,
        title: "🤯 Lobe Flow",
        url: "https://github.com/lobehub/lobe-flow",
      },
    ],
    title: "Resources",
  },
  {
    items: [
      {
        description: "AI Commit CLI",
        openExternal: true,
        title: "💌 Lobe Commit",
        url: "https://github.com/lobehub/lobe-commit",
      },
      {
        description: "Lint Config",
        openExternal: true,
        title: "📐 Lobe Lint",
        url: "https://github.com/lobehub/lobe-lint",
      },
    ],
    title: "More Products",
  },
];

const MainLayout: React.FC = () => {
  return (
    <Layout
      header={<Header actions={"ACTIONS"} logo={"LOGO"} nav={"NAV"} />}
      footer={<Footer bottom="Copyright © 2022" columns={columns} />}
    >
      <div></div>
    </Layout>
  );
};

export default MainLayout;
