import * as z from 'zod';
import { Logger } from '../../utils/logger.js';
import { McpUnity } from '../../unity/mcpUnity.js';
import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { McpUnityError, ErrorType } from '../../utils/errors.js';
import { CallToolResult } from '@modelcontextprotocol/sdk/types.js';

const toolName = 'add_asset_to_project';
const toolDescription = 'Imports assets (images, etc.) into the Unity project';
const paramsSchema = z.object({
  sourcePaths: z.union([
    z.string(),
    z.array(z.string())
  ]).describe('The source path(s) of the asset(s) to import (file not directory)'),
  destPath: z.string().describe('The destination path where the asset(s) will be imported to')
});

export function registerAddAssetTool(server: McpServer, mcpUnity: McpUnity, logger: Logger) {
  logger.info(`Registering tool: ${toolName}`);
      
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

async function toolHandler(mcpUnity: McpUnity, params: any): Promise<CallToolResult> {
  const { sourcePaths, destPath } = params;
  
  // Convert single path to array if needed
  const paths = Array.isArray(sourcePaths) ? sourcePaths : [sourcePaths];
        
  if (!paths || !destPath) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'Required parameters "sourcePaths" and "destPath" must be provided'
    );
  }

  if (paths.length === 0) {
    throw new McpUnityError(
      ErrorType.VALIDATION,
      'At least one source path must be provided'
    );
  }

  const response = await mcpUnity.sendRequest({
    method: toolName,
    params: {
      sourcePaths: paths,
      destPath
    }
  });
  
  if (!response.success) {
    throw new McpUnityError(
      ErrorType.TOOL_EXECUTION,
      response.message || `Failed to import assets to ${destPath}`,
      response.errors
    );
  }

  return {
    content: [{
      type: response.type || 'text',
      text: response.message,
    }]
  };
}
