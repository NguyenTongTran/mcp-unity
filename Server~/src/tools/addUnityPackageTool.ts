import * as z from 'zod';
import { Logger } from '../utils/logger.js';
import { McpUnity } from '../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

// Constants for the tool
const toolName = 'add_unity_package';
const toolDescription = 'Add custom Unity package (.unitypackage) to the project';
const paramsSchema = z.object({
  packagePath: z.string().describe('The absolute path to the .unitypackage file to import')
});

/**
 * Creates and registers the Add Unity Package tool with the MCP server
 * This tool allows adding a Unity package (.unitypackage) to the project
 * 
 * @param server The MCP server instance to register with
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param logger The logger instance for diagnostic information
 */
export function registerAddUnityPackageTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
      
  // Register this tool with the MCP server
  server.tool(
    toolName,
    toolDescription,
    paramsSchema.shape,
    async (params: any) => {
      try {
        logger.info(`Executing tool: ${toolName}`, params);
        const result = await toolHandler(mcpUnity, params);
        logger.info(`Tool execution successful: ${toolName}`);
        return result;
      } catch (error) {
        logger.error(`Tool execution failed: ${toolName}`, error);
        throw error;
      }
    }
  );
}

/**
 * Handles add Unity package tool requests
 * 
 * @param mcpUnity The McpUnity instance to communicate with Unity
 * @param params The parameters for the tool
 * @returns A promise that resolves to the tool execution result
 * @throws McpUnityError if the request to Unity fails
 */
async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { packagePath } = params;
  if (!packagePath) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'Required parameter "packagePath" not provided'
    );
  }
  
  // Send to Unity
  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: { packagePath }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || 'Failed to import .unitypackage file.'
    );
  }
  
  return {
    content: [
      {
        type: 'text',
        text: response.message
      },
      {
        type: 'resource',
        resource: { 
          uri: "unity://assets",
          mimeType: "application/json",
          text: JSON.stringify(response.assets || [], null, 2)
        }
      }
    ]
  };
}
